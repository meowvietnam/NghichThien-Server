using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Game.Rank;
using MTS.Models;
using Network;
using Network.Extensions;
using static MTS.Utils.Utils;

namespace MTS.Services;

public class RechargeRankService : BackgroundService
{
    private static readonly TimeSpan RankInterval = TimeSpan.FromDays(7);
    private readonly ILogger<RechargeRankService> logger;
    public static List<RechargeRankEntry> Tops { get; private set; } = new();

    public RechargeRankService(ILogger<RechargeRankService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunJobAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi khởi tạo {Service}", nameof(RechargeRankService));
        }

        _ = Task.Run(() => GrantRewardsLoop(stoppingToken), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();
            logger.LogInformation(
                "{Service} sẽ chạy sau {Delay}", nameof(RechargeRankService), delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(RechargeRankService));
            }
        }
    }

    private static TimeSpan GetDelayToNextRun()
    {
        return TimeSpan.FromMinutes(2);
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        var periodStart = GetCurrentPeriodStart(Now());
        await GetTopsAsync(periodStart, periodStart + RankInterval, stoppingToken);
    }
    private record RewardPercent(int Top, int Percent);
    private async Task GrantRewardsLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToGrantRewards();
            logger.LogInformation(
                "{Service} sẽ phát thưởng sau {Delay}", nameof(RechargeRankService), delay);
            await Task.Delay(delay, stoppingToken);
            var periodEnd = GetCurrentPeriodStart(Now());
            try
            {
                await GetTopsAsync(periodEnd - RankInterval, periodEnd, stoppingToken);
                var rewardPercents = new RewardPercent[]
                {
                    new(0,100),
                    new (1,50),
                    new (2,30),
                    new(3,10),
                };
                var rewards = Tops.Select((x, i) =>
                {
                    var percent = rewardPercents.Last(x => x.Top <= i).Percent / 100f;
                    var moneyVip = (int)(x.TotalSpent * percent);
                    return new TopReward(new[] { x.CharacterWrapper.Character }, moneyVip, $"Top {i + 1}");
                }).ToArray();
                await RankRewardMailer.SendAsync(rewards, "Tổng Nạp", stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi phát thưởng {Service}", nameof(RechargeRankService));
            }
        }
    }

    public static TimeSpan GetDelayToGrantRewards()
    {
        var start = GameConfig.GameReleaseDate;
        var now = Now();
        var next = GetNextPeriodicTime(start, now, RankInterval);
        return next - now;
    }

    private static DateTimeOffset GetCurrentPeriodStart(DateTimeOffset now)
    {
        var start = GameConfig.GameReleaseDate;
        if (now <= start)
        {
            return start;
        }

        var passedPeriods = (now - start).Ticks / RankInterval.Ticks;
        return start.AddTicks(passedPeriods * RankInterval.Ticks);
    }

    private async Task GetTopsAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken stoppingToken)
    {
        await using var db = await Program.CreateDbAsync();
        var startUtc = periodStart.UtcDateTime;
        var endUtc = periodEnd.UtcDateTime;
        var totals = await db.IapPurchases
            .AsNoTracking()
            .Where(x => x.PurchaseDate >= startUtc && x.PurchaseDate < endUtc)
            .GroupBy(x => x.CharId)
            .Select(x => new
            {
                CharId = x.Key,
                TotalSpent = x.Sum(p => p.MoneyVipSnapshot)
            })
            .OrderByDescending(x => x.TotalSpent)
            .ThenBy(x => x.CharId)
            .Take(10)
            .ToListAsync(stoppingToken);

        if (totals.Count == 0)
        {
            Tops = new();
            return;
        }

        var charIds = totals.Select(x => x.CharId).ToList();
        var characters = await db.Characters
            .AsNoTracking()
            .Include(x => x.Appearance)
            .Include(x => x.Progression)
            .Include(x => x.Stats)
            .Where(x => charIds.Contains(x.Id))
            .AsSplitQuery()
            .ToDictionaryAsync(x => x.Id, stoppingToken);

        Tops = totals
            .Where(x => characters.ContainsKey(x.CharId))
            .Select(x => new RechargeRankEntry(characters[x.CharId], x.TotalSpent))
            .ToList();
    }
}
