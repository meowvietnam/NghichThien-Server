using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Game.Rank;
using MTS.Models;
using MTS.Network;
using static MTS.Utils.Utils;

namespace MTS.Services;

public class ArenaRankService : BackgroundService
{
    private readonly ILogger<ArenaRankService> logger;
    public static List<CharacterWrapper> Tops { get; private set; } = new();

    public ArenaRankService(ILogger<ArenaRankService> logger)
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
            logger.LogInformation(
                "{Service} sẽ chạy sau {Delay}", nameof(ArenaRankService), delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(ArenaRankService));
            }
        }
    }


    private async Task GrantRewardsLoop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToGrantRewards();
            logger.LogInformation(
                "{Service} sẽ phát thưởng sau {Delay}", nameof(ArenaRankService), delay);
            await Task.Delay(delay, stoppingToken);
            try
            {
                await GetTopsAsync(stoppingToken);
                var rewards = new TopReward[]
                {
                    new(Tops.Take(1).Select(x => x.Character).ToArray(), 2000, "Top 1"),
                    new(Tops.Skip(1).Take(1).Select(x => x.Character).ToArray(), 1000, "Top 2"),
                    new(Tops.Skip(2).Take(1).Select(x => x.Character).ToArray(), 500, "Top 3"),
                    new(Tops.Skip(3).Take(7).Select(x => x.Character).ToArray(), 300, "Top 4-10"),
                };
                await RankRewardMailer.SendAsync(rewards, "Đấu Trường", stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi phát thưởng {Service}", nameof(ArenaRankService));
            }
        }
    }

    private static TimeSpan GetDelayToNextRun()
    {
        return TimeSpan.FromMinutes(2);
    }

    public static TimeSpan GetDelayToGrantRewards()
    {
        var start = GameConfig.GameReleaseDate;
        var now = Now();
        var next = GetNextPeriodicTime(start, now, TimeSpan.FromDays(14));
        return next - now;
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
            .Include(x => x.ArenaManager)
            .OrderByDescending(x => x.ArenaManager.Points)
            .ThenByDescending(x => x.Progression.Level)
            .ThenByDescending(x => x.Progression.CurrentExp)
            .Take(10)
            .Select(x => new CharacterWrapper(x))
            .AsSplitQuery()
            .ToListAsync(stoppingToken);
    }
}
