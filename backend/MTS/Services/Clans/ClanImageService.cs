using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MTS.Data;
using MTS.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace MTS.Services.Clans;

public sealed class ClanImageService(IDbContextFactory<AppDbContext> factory, CloudflareR2Service r2,
    IOptions<CloudflareR2Options> options)
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public static byte[] Normalize(byte[] bytes)
    {
        if (bytes.Length == 0 || bytes.Length > MaxBytes) throw new ClanRuleException("InvalidImageSize");
        var decoder = new DecoderOptions { MaxFrames = 1, SkipMetadata = false };
        var info = Image.Identify(decoder, bytes);
        if (info.Width > 4096 || info.Height > 4096) throw new ClanRuleException("ImageDimensionsTooLarge");
        var format = Image.DetectFormat(bytes).Name;
        if (format is not ("JPEG" or "PNG" or "WEBP")) throw new ClanRuleException("UnsupportedImageFormat");
        using var image = Image.Load(decoder, bytes);
        image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions {
            Size = new Size(128, 128), Mode = ResizeMode.Crop, Position = AnchorPositionMode.Center }));
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;
        using var output = new MemoryStream();
        image.SaveAsPng(output);
        return output.ToArray();
    }

    public async Task Upload(int charId, int clanId, byte[] bytes, CancellationToken cancellationToken)
    {
        using var initial = factory.CreateDbContext();
        var clan = await initial.Clans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clanId, cancellationToken);
        if (clan == null || clan.Status != ClanStatus.Active || clan.LeaderCharId != charId)
            throw new ClanRuleException("Forbidden");
        if (!Uri.TryCreate(options.Value.PublicBaseUrl, UriKind.Absolute, out var publicUrl) || publicUrl.Scheme != "https")
            throw new ClanRuleException("ImageStorageNotConfigured");
        var png = Normalize(bytes);
        var key = $"clans/{clanId}/{Guid.NewGuid():N}.png";
        if (options.Value.PublicBaseUrl!.TrimEnd('/').Length + key.Length + 1 > 512)
            throw new ClanRuleException("ImageStorageNotConfigured");
        var uploaded = await r2.UploadAsync(png, key, "image/png", cancellationToken);
        using var strategyDb = factory.CreateDbContext();
        await strategyDb.Database.CreateExecutionStrategy().ExecuteAsync(async () => {
            using var db = factory.CreateDbContext();
            using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var current = await db.Clans.SingleAsync(x => x.Id == clanId, cancellationToken);
            if (current.ImageUrl == uploaded.PublicUrl) return; // commit acknowledgement lost
            if (current.Status != ClanStatus.Active || current.LeaderCharId != charId)
                throw new ClanRuleException("Forbidden");
            if (current.Version != clan.Version) throw new ClanRuleException("StaleVersion");
            current.ImageUrl = uploaded.PublicUrl;
            current.Version++;
            current.UpdatedAt = DateTime.UtcNow;
            var eventId = Guid.NewGuid();
            db.ClanAuditLogs.Add(new ClanAuditLog { ClanId = clanId, ActorCharId = charId, Action = "UploadImage",
                RequestId = eventId, CreatedAt = current.UpdatedAt });
            db.ClanOutboxEvents.Add(new ClanOutboxEvent { EventId = eventId, ClanId = clanId, TargetCharId = charId,
                ClanVersion = current.Version, EventType = "UploadImage", CreatedAt = current.UpdatedAt, NextAttemptAt = current.UpdatedAt });
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        });
    }
}
