using MTS.Game;
using static MTS.Utils.Utils;
namespace MTS.Services;

public class ArenaService : BackgroundService
{
    private readonly ILogger<ArenaService> logger;

    public ArenaService(ILogger<ArenaService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = GetDelayToGrantRewards();
        logger.LogInformation(
            "{Service} sẽ chạy sau {Delay}", nameof(ArenaService), delay);
        await Task.Delay(delay,stoppingToken);
        try
        {
            await RunJobAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(TuViRankService));
        }
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
    }
}