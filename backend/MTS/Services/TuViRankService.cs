using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Models;

namespace MTS.Services;

using static MTS.Utils.Utils;
public class TuViRankService : BackgroundService
{
    private readonly ILogger<TuViRankService> logger;
    public List<CharacterData> Tops { get; private set; }
    public TuViRankService(ILogger<TuViRankService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(GrantRewards, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();
            logger.LogInformation(
                "{Service} sẽ chạy sau {Delay}", nameof(TuViRankService), delay);
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
    private record TopReward(List<CharacterData> Tops, int MoneyVip);
    private async Task GrantRewards()
    {
        var delay = GetDelayToGrantRewards();
        await Task.Delay(delay);
        await GetTopsAsync(CancellationToken.None);
        var tops = Tops;
        var top1 = tops.Take(1).ToList();
        var top2 = tops.Skip(1).Take(1).ToList();
        var top3 = tops.Skip(2).Take(1).ToList();
        var top4To10 = tops.Skip(3).Take(7).ToList();
        var rewards = new TopReward[]
        {
            new (top1, 1000),
            new (top2, 500),
            new (top3, 300),
            new(top4To10, 100),
        };
    }

    private static TimeSpan GetDelayToGrantRewards()
    {
        var start = GameConfig.GameReleaseDate;
        var now = Now();
        var next = GetNextPeriodicTime(start, now, TimeSpan.FromDays(7));
        return next - now;
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
            .AsSplitQuery()
            .ToListAsync(stoppingToken);
    }

}