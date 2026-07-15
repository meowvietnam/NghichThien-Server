using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Game.Rank;
using MTS.Models;
using MTS.Network;

namespace MTS.Services;

using static MTS.Utils.Utils;
public class TuViRankService : BackgroundService
{
    private readonly ILogger<TuViRankService> logger;
    public static List<CharacterWrapper> Tops { get; private set; } = new();
    private static readonly TimeSpan Interval = TimeSpan.FromDays(7);
    public TuViRankService(ILogger<TuViRankService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunJobAsync(stoppingToken);
        _ = Task.Run(() => GrantRewardsLoop(stoppingToken), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();
            // logger.LogInformation(
            //     "{Service} sẽ chạy sau {Delay}", nameof(TuViRankService), delay);
            await Task.Delay(delay, stoppingToken);
            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(TuViRankService));
            }
        }
    }
    private async Task GrantRewardsLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToGrantRewards();
            logger.LogInformation(
                "{Service} sẽ phát thưởng sau {Delay}", nameof(TuViRankService), delay);
            await Task.Delay(delay, stoppingToken);
            try
            {
                await GetTopsAsync(stoppingToken);
                var rewards = new TopReward[]
                {
                    new(Tops.Take(1).Select(x => x.Character).ToArray(), 1000, "Top 1"),
                    new(Tops.Skip(1).Take(1).Select(x => x.Character).ToArray(), 500, "Top 2"),
                    new(Tops.Skip(2).Take(1).Select(x => x.Character).ToArray(), 300, "Top 3"),
                    new(Tops.Skip(3).Take(7).Select(x => x.Character).ToArray(), 100, "Top 4-10"),
                };
                await RankRewardMailer.SendAsync(rewards, "Tu Vi", stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi phát thưởng {Service}", nameof(TuViRankService));
            }
        }
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

        var passedPeriods = (now - start).Ticks / Interval.Ticks;
        return start.AddTicks(passedPeriods * Interval.Ticks);
    }

    public static DateTimeOffset GetTimeEnd()
    {
        return GetTimeStart() + Interval;
    }

    private static TimeSpan GetDelayToNextRun()
    {
        return TimeSpan.FromMinutes(2);
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        await GetTopsAsync(stoppingToken);
    }

    private async Task GetTopsAsync(CancellationToken stoppingToken)
    {
        await using var db = await Program.CreateDbAsync();
        Tops = await db.Characters
            .AsNoTracking()
            .Include(x => x.Appearance)
            .Include(x => x.Progression)
            .Include(x => x.Stats)
            .OrderByDescending(x => x.Progression.Level)
            .ThenByDescending(x => x.Progression.CurrentExp)
            .ThenByDescending(x => x.Stats.SinhMenh)
            .Take(10)
            .Select(x => new CharacterWrapper(x))
            .AsSplitQuery()
            .ToListAsync(stoppingToken);
    }

}
