using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Network;

namespace MTS.Services;

public class ReloadDailyQuestService : BackgroundService
{
    private readonly ILogger<ReloadDailyQuestService> logger;

    public ReloadDailyQuestService(ILogger<ReloadDailyQuestService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();
            delay = TimeSpan.FromSeconds(6);
            logger.LogInformation(
                "{Service} sẽ chạy sau {Delay}", nameof(ReloadDailyQuestService), delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(ReloadDailyQuestService));
            }
        }
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        const int batchSize = 500;
        const int maxWorkers = 4;
        var processedCount = 0;
        var failedCount = 0;
        var lastCharId = 0;
        var stopwatch = Stopwatch.StartNew();

        await using (var db = await Program.CreateDbAsync())
        {
            var totalCount = await db.QuestManagers.CountAsync(stoppingToken);
            logger.LogInformation(
                "Bắt đầu reload daily quest cho {TotalCount} nhân vật với {MaxWorkers} workers",
                totalCount,
                maxWorkers);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var db = await Program.CreateDbAsync();
            var charIds = await db.QuestManagers
                .Where(x => x.CharId > lastCharId)
                .OrderBy(x => x.CharId)
                .Select(x => x.CharId)
                .Take(batchSize)
                .ToListAsync(stoppingToken);
            if (charIds.Count == 0)
            {
                break;
            }

            lastCharId = charIds[^1];
            var onlineChars = TcpServer.Instance
                .GetCharsOnline()
                .ToDictionary(x => x.Id);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxWorkers,
                CancellationToken = stoppingToken
            };

            await Parallel.ForEachAsync(charIds, options, async (charId, token) =>
            {
                try
                {
                    await using var dbContext = await Program.CreateDbAsync();
                    if (onlineChars.TryGetValue(charId, out var charData))
                    {
                        lock (charData)
                        {
                            charData.QuestManager.ReloadDailyQuests();
                        }
                        await charData.QuestManager.SaveChanges(dbContext);
                    }
                    else
                    {
                        var questManager = await dbContext.QuestManagers
                            .AsNoTracking()
                            .FirstAsync(x => x.CharId == charId, token);
                        await questManager.Load(dbContext);
                        questManager.ReloadDailyQuests();
                        await questManager.SaveChanges(dbContext);
                    }

                    Interlocked.Increment(ref processedCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedCount);
                    logger.LogError(
                        ex,
                        "Không thể reload daily quest cho nhân vật {CharacterId}",
                        charId);
                }
            });

            logger.LogInformation(
                "Đã reload daily quest: {ProcessedCount} thành công, {FailedCount} thất bại; batch hiện tại {BatchCount} nhân vật",
                processedCount,
                failedCount,
                charIds.Count);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Hoàn tất reload daily quest: {ProcessedCount} thành công, {FailedCount} thất bại trong {Elapsed}",
            processedCount,
            failedCount,
            stopwatch.Elapsed);
    }

    private static TimeSpan GetDelayToNextRun()
    {
        var utc7 = TimeSpan.FromHours(7);
        var now = DateTimeOffset.UtcNow.ToOffset(utc7);
        var nextMidnight = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            0, 0, 0,
            utc7).AddDays(1);
        return nextMidnight - now;
    }
}
