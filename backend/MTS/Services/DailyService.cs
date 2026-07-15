using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Network;
using MTS.Network.Models;
using static MTS.Utils.Utils;

namespace MTS.Services;

public class DailyService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private readonly ILogger<DailyService> logger;

    public DailyService(ILogger<DailyService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToRun();
            logger.LogInformation(
                "{Service} sẽ chạy sau {Delay}", nameof(DailyService), delay);
            await Task.Delay(delay, stoppingToken);
            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(DailyService));
            }
        }
    }

    public static TimeSpan GetDelayToRun()
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

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "{Service} chạy daily job trong khoảng {TimeStart} - {TimeEnd}",
            nameof(DailyService),
            GetTimeStart(),
            GetTimeEnd());
        await using var db = await Program.CreateDbAsync();
        var reset = new DailyReset();
        await db.ArenaManagers
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.CurrentArenaCount, reset.CurrentArenaCount),
                stoppingToken);
        await db.QuestManagers
            .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.CurrentCountSideQuest, reset.CurrentCountSideQuest),
                stoppingToken);
        var onlineChars = TcpServer.Instance
            .GetCharsOnline()
            .ToDictionary(x => x.Id);
        foreach (var charData in onlineChars.Values)
        {
            lock (charData)
            {
                charData.ApplyReset(reset);
                charData.Client.MemSend(MessageTag.DailyReset, reset);
            }
        }
    }
}