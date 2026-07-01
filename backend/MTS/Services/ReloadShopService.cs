using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Network;

namespace MTS.Services;

public class ReloadShopService : BackgroundService
{
    private readonly ILogger<ReloadShopService> logger;
    private readonly IServiceScopeFactory scopeFactory;

    public ReloadShopService(ILogger<ReloadShopService> logger, IServiceScopeFactory scopeFactory)
    {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();
            logger.LogInformation(
                "{Service} sẽ chạy sau {Delay}", nameof(ReloadShopService), delay);
            await Task.Delay(delay, stoppingToken);
            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(ReloadShopService));
            }
        }
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        var batchSize = 500;
        var maxWorkers = 4;
        var processedCount = 0;
        var failedCount = 0;
        var lastId = 0;
        var stopwatch = Stopwatch.StartNew();

        await using (var db = await Program.CreateDbAsync())
        {
            var totalCount = await db.Characters.CountAsync(stoppingToken);
            logger.LogInformation(
                "Bắt đầu reload shop cho {TotalCount} nhân vật với {MaxWorkers} workers",
                totalCount,
                maxWorkers);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var db = await Program.CreateDbAsync();
            var ids = await db.Characters
                .Where(x => x.Id > lastId)
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .Take(batchSize)
                .ToListAsync(stoppingToken);
            if (ids.Count == 0)
            {
                break;
            }
            lastId = ids[^1];

            var onlineChars = TcpServer.Instance
                .GetCharsOnline().ToDictionary(x => x.Id);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxWorkers,
                CancellationToken = stoppingToken
            };
            await Parallel.ForEachAsync(ids, options, async (i, token) =>
            {
                try
                {
                    await using var dbContext = await Program.CreateDbAsync();
                    if (!onlineChars.TryGetValue(i, out var charData))
                    {
                        charData = await dbContext.Characters
                            .AsNoTracking()
                            .Include(x => x.ShopManager)
                            .FirstAsync(x => x.Id == i, token);
                    }
                    var shopManager = charData.ShopManager;
                    shopManager.ReloadAll();
                    await shopManager.SaveChanges(dbContext);
                    Interlocked.Increment(ref processedCount);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedCount);
                    logger.LogError(ex, "Không thể reload shop cho nhân vật {CharacterId}", i);
                }
            });

            logger.LogInformation(
                "Đã reload shop: {ProcessedCount} thành công, {FailedCount} thất bại; batch hiện tại {BatchCount} nhân vật",
                processedCount,
                failedCount,
                ids.Count);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Hoàn tất reload shop: {ProcessedCount} thành công, {FailedCount} thất bại trong {Elapsed}",
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
