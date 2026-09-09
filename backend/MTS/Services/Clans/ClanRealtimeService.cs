using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using Network.Models.Clans;

namespace MTS.Services.Clans;

// Per-character routing: no full online-player scan. Payload contains invalidation only.
public sealed class ClanRealtimeService
{
    private readonly ConcurrentDictionary<int, Action<ClanChangedDto>> listeners = new();
    public void Subscribe(int charId, Action<ClanChangedDto> callback) => listeners[charId] = callback;
    public void Unsubscribe(int charId, Action<ClanChangedDto> callback) =>
        ((ICollection<KeyValuePair<int, Action<ClanChangedDto>>>)listeners).Remove(new(charId, callback));
    public void Publish(IEnumerable<int> charIds, ClanChangedDto change)
    {
        foreach (var id in charIds.Distinct())
            if (listeners.TryGetValue(id, out var callback)) callback(change);
    }
}

// One dispatcher per game server process. Snapshot sync recovers missed invalidations.
public sealed class ClanOutboxWorker(IDbContextFactory<AppDbContext> factory, ClanRealtimeService realtime,
    ILogger<ClanOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var db = await factory.CreateDbContextAsync(stoppingToken);
                var now = DateTime.UtcNow;
                var batch = await db.ClanOutboxEvents.Where(x => x.PublishedAt == null && x.NextAttemptAt <= now)
                    .OrderBy(x => x.Id).Take(100).ToListAsync(stoppingToken);
                foreach (var group in batch.GroupBy(x => x.ClanId))
                {
                    var ids = await db.ClanMembers.Where(x => x.ClanId == group.Key).Select(x => x.CharId).ToArrayAsync(stoppingToken);
                    // Coalesce bursts to one invalidation; include removed members/applicants for this batch.
                    var latest = group.MaxBy(x => x.ClanVersion)!;
                    var recipients = ids.Concat(group.Where(x => x.TargetCharId.HasValue).Select(x => x.TargetCharId!.Value));
                    try
                    {
                        realtime.Publish(recipients, new ClanChangedDto { EventId = latest.EventId,
                            ClanId = group.Key, Version = latest.ClanVersion });
                        foreach (var item in group) item.PublishedAt = now;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Clan invalidation delivery failed for {ClanId}", group.Key);
                        foreach (var item in group)
                        {
                            item.RetryCount++;
                            item.NextAttemptAt = now.AddSeconds(Math.Min(60, item.RetryCount * 2));
                        }
                    }
                }
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Clan outbox failed; verify the Clan migration is installed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            await Task.Delay(250, stoppingToken);
        }
    }
}
