using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.Clan;
using MTS.Game.DP;
using MTS.Game.Gacha;
using MTS.Game.LuyenKhi;
using MTS.Game.Resources;
using MTS.IAP;
using MTS.Models;
using MTS.ND;
using MTS.Network.Models;
using MTS.Utils;
using Newtonsoft.Json;
using Serilog;
using NgoDao = MTS.ND.NgoDao;

namespace MTS.Services;

public class Startup : BackgroundService
{
    private readonly ILogger<Startup> logger;

    public Startup(ILogger<Startup> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var db = await Program.CreateDbAsync();
    }

    private async Task Gift(AppDbContext db)
    {
        var product = await db.IapProducts.FirstOrDefaultAsync(x => x.Id == 8);
        var mail = new Mail()
        {
            CharId = 6191,
            Title = "Phần thưởng nạp",
            Content = "Phần thưởng nạp",
            Rewards = product.Reward.GetMailRewards()
        };
        await db.Mails.AddAsync(mail);
        await db.SaveChangesAsync();
        Log.Information("oke");
    }
    private async Task RemoveEvents(AppDbContext db)
    {
        await db.Events.ExecuteDeleteAsync();
        await db.SaveChangesAsync();
        Log.Information("oke");
    }


    private async Task AddEvents(AppDbContext db)
    {
        var charIds = await db.EventManagers
            .Select(x => x.CharId)
            .ToArrayAsync();

        foreach (var charId in charIds)
        {
            var events = EventService.CreateEvents();
            foreach (var @event in events)
            {
                @event.CharId = charId;
                await db.Events.AddAsync(@event);
            }
        }

        await db.SaveChangesAsync();
        Log.Information("Oke");
    }

    private async Task ForceMoveOverflowClanMembers(
        AppDbContext db,
        int sourceClanId,
        int targetClanId,
        CancellationToken stoppingToken = default)
    {
        if (sourceClanId <= 0) throw new ArgumentOutOfRangeException(nameof(sourceClanId));
        if (targetClanId <= 0) throw new ArgumentOutOfRangeException(nameof(targetClanId));
        if (sourceClanId == targetClanId)
        {
            throw new ArgumentException("Clan nguồn và clan đích không được trùng nhau.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
        var sourceClan = await db.Clans
                             .FirstOrDefaultAsync(x => x.Id == sourceClanId, stoppingToken)
                         ?? throw new InvalidOperationException($"Không tìm thấy clan nguồn {sourceClanId}.");
        var targetClan = await db.Clans
                             .FirstOrDefaultAsync(x => x.Id == targetClanId, stoppingToken)
                         ?? throw new InvalidOperationException($"Không tìm thấy clan đích {targetClanId}.");

        var sourceMembers = await db.ClanMembers
            .Include(x => x.CharData)
            .ThenInclude(x => x.Progression)
            .Where(x => x.ClanId == sourceClanId)
            .ToListAsync(stoppingToken);
        var targetMemberCount = await db.ClanMembers
            .CountAsync(x => x.ClanId == targetClanId, stoppingToken);
        var overflowCount = sourceMembers.Count - sourceClan.MaxMembers;
        if (overflowCount <= 0)
        {
            sourceClan.MemberCount = sourceMembers.Count;
            targetClan.MemberCount = targetMemberCount;
            await db.SaveChangesAsync(stoppingToken);
            await transaction.CommitAsync(stoppingToken);
            Log.Information("Clan {SourceClanId} không quá slot: {MemberCount}/{MaxMembers}.",
                sourceClanId,
                sourceMembers.Count,
                sourceClan.MaxMembers);
            return;
        }

        var targetFreeSlots = Math.Max(0, targetClan.MaxMembers - targetMemberCount);
        var moveCount = Math.Min(overflowCount, targetFreeSlots);
        if (moveCount <= 0)
        {
            sourceClan.MemberCount = sourceMembers.Count;
            targetClan.MemberCount = targetMemberCount;
            await db.SaveChangesAsync(stoppingToken);
            await transaction.CommitAsync(stoppingToken);
            Log.Warning("Clan đích {TargetClanId} không còn slot trống: {MemberCount}/{MaxMembers}.",
                targetClanId,
                targetMemberCount,
                targetClan.MaxMembers);
            return;
        }

        var movedMembers = sourceMembers
            .Where(x => x.Role != ClanRole.Leader && x.CharId != sourceClan.LeaderCharId)
            .OrderBy(x => x.ContribPoints)
            .ThenBy(x => x.CharData.Progression.Level)
            .ThenBy(x => x.JoinAt)
            .Take(moveCount)
            .ToList();
        if (movedMembers.Count <= 0)
        {
            Log.Warning("Clan {SourceClanId} quá slot nhưng không có thành viên hợp lệ để chuyển.", sourceClanId);
            return;
        }

        var movedCharIds = movedMembers.Select(x => x.CharId).ToArray();
        await db.ClanJoinRequests
            .Where(x => x.ClanId == targetClanId && movedCharIds.Contains(x.CharId))
            .ExecuteDeleteAsync(stoppingToken);

        var movedAt = DateTime.UtcNow;
        var targetMembers = movedMembers
            .Select(x => new ClanMember
            {
                ClanId = targetClanId,
                CharId = x.CharId,
                ContribPoints = x.ContribPoints,
                LastFreeContributionAt = x.LastFreeContributionAt,
                Role = ClanRole.Member,
                JoinAt = movedAt
            })
            .ToList();
        db.ClanMembers.RemoveRange(movedMembers);
        await db.ClanMembers.AddRangeAsync(targetMembers, stoppingToken);

        sourceClan.MemberCount = sourceMembers.Count - movedMembers.Count;
        targetClan.MemberCount = targetMemberCount + movedMembers.Count;
        await db.SaveChangesAsync(stoppingToken);
        await transaction.CommitAsync(stoppingToken);

        Log.Information(
            "Đã chuyển {MoveCount} thành viên từ clan {SourceClanId} sang clan {TargetClanId}: {CharIds}.",
            movedMembers.Count,
            sourceClanId,
            targetClanId,
            string.Join(", ", movedCharIds));
    }

    private async Task ForceMoveAllOverflowClanMembers(
        AppDbContext db,
        CancellationToken stoppingToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(stoppingToken);
        var clans = await db.Clans
            .OrderBy(x => x.Id)
            .ToListAsync(stoppingToken);
        var memberCounts = await db.ClanMembers
            .GroupBy(x => x.ClanId)
            .Select(x => new { ClanId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.ClanId, x => x.Count, stoppingToken);
        var clanById = clans.ToDictionary(x => x.Id);
        var currentCounts = clans.ToDictionary(
            x => x.Id,
            x => memberCounts.TryGetValue(x.Id, out var count) ? count : 0);
        var freeSlots = clans.ToDictionary(
            x => x.Id,
            x => Math.Max(0, x.MaxMembers - currentCounts[x.Id]));
        var overflowClans = clans
            .Where(x => currentCounts[x.Id] > x.MaxMembers)
            .OrderByDescending(x => currentCounts[x.Id] - x.MaxMembers)
            .ThenBy(x => x.Id)
            .ToList();
        var totalMoved = 0;
        foreach (var sourceClan in overflowClans)
        {
            var overflowCount = currentCounts[sourceClan.Id] - sourceClan.MaxMembers;
            if (overflowCount <= 0) continue;

            var sourceMembers = await db.ClanMembers
                .Include(x => x.CharData)
                .ThenInclude(x => x.Progression)
                .Where(x => x.ClanId == sourceClan.Id)
                .ToListAsync(stoppingToken);
            var candidates = sourceMembers
                .Where(x => x.Role != ClanRole.Leader && x.CharId != sourceClan.LeaderCharId)
                .OrderBy(x => x.ContribPoints)
                .ThenBy(x => x.CharData.Progression.MaxLevel)
                .ThenBy(x => x.JoinAt)
                .Take(overflowCount)
                .ToList();
            if (candidates.Count <= 0)
            {
                Log.Warning("Clan {SourceClanId} quá slot nhưng không có thành viên hợp lệ để chuyển.",
                    sourceClan.Id);
                continue;
            }

            foreach (var member in candidates)
            {
                var targetClan = clans
                    .Where(x => x.Id != sourceClan.Id && freeSlots[x.Id] > 0)
                    .OrderByDescending(x => freeSlots[x.Id])
                    .ThenBy(x => currentCounts[x.Id])
                    .ThenBy(x => x.Id)
                    .FirstOrDefault();
                if (targetClan == null)
                {
                    Log.Warning("Không còn clan đích có slot trống để chuyển tiếp từ clan {SourceClanId}.",
                        sourceClan.Id);
                    break;
                }

                await db.ClanJoinRequests
                    .Where(x => x.ClanId == targetClan.Id && x.CharId == member.CharId)
                    .ExecuteDeleteAsync(stoppingToken);
                db.ClanMembers.Remove(member);
                await db.ClanMembers.AddAsync(new ClanMember
                {
                    ClanId = targetClan.Id,
                    CharId = member.CharId,
                    ContribPoints = member.ContribPoints,
                    LastFreeContributionAt = member.LastFreeContributionAt,
                    Role = ClanRole.Member,
                    JoinAt = DateTime.UtcNow
                }, stoppingToken);

                currentCounts[sourceClan.Id]--;
                currentCounts[targetClan.Id]++;
                freeSlots[targetClan.Id]--;
                totalMoved++;
                Log.Information(
                    "Chuyển CharId {CharId} từ clan {SourceClanId} sang clan {TargetClanId}.",
                    member.CharId,
                    sourceClan.Id,
                    targetClan.Id);
            }
        }

        foreach (var (clanId, count) in currentCounts)
        {
            if (clanById.TryGetValue(clanId, out var clan))
            {
                clan.MemberCount = count;
            }
        }

        await db.SaveChangesAsync(stoppingToken);
        await transaction.CommitAsync(stoppingToken);
        Log.Information("Quét quá slot tông môn xong, đã chuyển {TotalMoved} thành viên.", totalMoved);
    }
}