using MTS.Game.TMC;
using Serilog;

namespace MTS.Services;

/// <summary>Khởi tạo/khôi phục Tông môn chiến, chạy nhịp di chuyển/trận và ghi bền định kỳ.</summary>
public class TongMonChienService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WarManager.Initialize();
                break;
            }
            catch (Exception e)
            {
                Log.Error(e, "TongMonChien initialize failed; retry in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        var lastFlush = DateTime.UtcNow;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                WarManager.Tick();
                if (DateTime.UtcNow - lastFlush >= FlushInterval)
                {
                    lastFlush = DateTime.UtcNow;
                    await WarManager.Flush();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "TongMonChien tick failed");
            }

            try
            {
                await Task.Delay(TickInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await WarManager.Flush();
    }
}
