using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Game.Rank;
using MTS.Models;
using static MTS.Utils.Utils;

namespace MTS.Services;


public class MoneyVipSpendingRankService : BackgroundService
{
    public static readonly TimeSpan RankInterval = TimeSpan.FromDays(7);
    private readonly ILogger<MoneyVipSpendingRankService> logger;
    public static List<MoneyVipSpendingRankEntry> Tops { get; private set; } = new();

    public MoneyVipSpendingRankService(ILogger<MoneyVipSpendingRankService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RefreshAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi khởi tạo {Service}", nameof(MoneyVipSpendingRankService));
        }

        _ = Task.Run(() => GrantRewardsLoop(stoppingToken), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeSpan.FromMinutes(2);
            // logger.LogInformation(
            //     "{Service} sẽ chạy sau {Delay}", nameof(MoneyVipSpendingRankService), delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(MoneyVipSpendingRankService));
            }
        }
    }

    private async Task RefreshAsync(CancellationToken stoppingToken)
    {
        Tops = await GetTopsAsync(GetTimeStart(), GetTimeEnd(), stoppingToken);
    }

    private record RewardPercent(int Top, int Percent);

    private async Task GrantRewardsLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToGrantRewards().Add(TimeSpan.FromSeconds(3));
            logger.LogInformation(
                "{Service} sẽ phát thưởng sau {Delay}", nameof(MoneyVipSpendingRankService), delay);
            await Task.Delay(delay, stoppingToken);
            var periodEnd = GetTimeStart();
            try
            {
                Tops = await GetTopsAsync(periodEnd - RankInterval, periodEnd, stoppingToken);
                await GrantRewards(Tops, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi phát thưởng {Service}", nameof(MoneyVipSpendingRankService));
            }
        }
    }

    public async Task GrantRewards(List<MoneyVipSpendingRankEntry> tops, CancellationToken stoppingToken)
    {
        var rewardPercents = new RewardPercent[]
        {
            new(0, 50),
            new(1, 30),
            new(2, 10),
            new(3, 5),
        };
        var rewards = tops.Select((entry, index) =>
        {
            var percent = rewardPercents.Last(x => x.Top <= index).Percent / 100f;
            var moneyVip = (int)(entry.TotalSpent * percent);
            return new TopReward([entry.CharacterWrapper.Character], moneyVip, $"Top {index + 1}");
        }).ToArray();
        await RankRewardMailer.SendAsync(rewards, "Tiêu Tiên Ngọc", stoppingToken);
    }
    public static TimeSpan GetDelayToGrantRewards()
    {
        var now = Now();
        return GetTimeEnd() - now;
    }

    public static DateTimeOffset GetTimeStart()
    {
        var start = GameConfig.GameReleaseDate;
        var now = Now();
        if (now <= start)
        {
            return start;
        }

        var passedPeriods = (now - start).Ticks / RankInterval.Ticks;
        return start.AddTicks(passedPeriods * RankInterval.Ticks);
    }

    public static DateTimeOffset GetTimeEnd()
    {
        return GetTimeStart() + RankInterval;
    }

    public async Task<List<MoneyVipSpendingRankEntry>> GetTopsAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken stoppingToken)
    {
        await using var db = await Program.CreateDbAsync();
        var startUtc = periodStart.UtcDateTime;
        var endUtc = periodEnd.UtcDateTime;
        var totals = await db.SpendingMoneyVips
            .AsNoTracking()
            .Where(x => x.Time >= startUtc && x.Time < endUtc)
            .GroupBy(x => x.CharId)
            .Select(x => new
            {
                CharId = x.Key,
                TotalSpent = x.Sum(item => item.MoneyVip)
            })
            .OrderByDescending(x => x.TotalSpent)
            .ThenBy(x => x.CharId)
            .Take(10)
            .ToListAsync(stoppingToken);

        if (totals.Count == 0)
        {
            return new();
        }

        var charIds = totals.Select(x => x.CharId).ToList();
        var characters = await db.Characters
            .AsNoTracking()
            .Include(x => x.Appearance)
            .Include(x => x.Progression)
            .Include(x => x.Stats)
            .Include(x => x.ArenaManager)
            .Include(x => x.EquipmentManager)
            .ThenInclude(x => x.ThanThongEquips)
            .Include(x => x.EquipmentManager)
            .ThenInclude(x => x.DanCombatUseds)
            .Include(x => x.InventoryManager)
            .Where(x => charIds.Contains(x.Id))
            .AsSplitQuery()
            .ToDictionaryAsync(x => x.Id, stoppingToken);
        foreach (var character in characters.Values)
        {
            await character.InventoryManager.Load(db);
        }
        return totals
            .Where(x => characters.ContainsKey(x.CharId))
            .Select(x => new MoneyVipSpendingRankEntry(characters[x.CharId], x.TotalSpent))
            .ToList();
    }
}
