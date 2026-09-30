using MTS.Game.TMC;
using Serilog;

namespace MTS.Services;

/// <summary>Khởi tạo/khôi phục Tông môn chiến, chạy nhịp di chuyển/trận và ghi bền định kỳ.</summary>
public class TongMonChienService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SettlementCheckInterval = TimeSpan.FromSeconds(30);

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
        var lastSettlementCheck = DateTime.MinValue;
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

                if (DateTime.UtcNow - lastSettlementCheck >= SettlementCheckInterval)
                {
                    lastSettlementCheck = DateTime.UtcNow;
                    await WarManager.RunScheduledSettlements();
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
