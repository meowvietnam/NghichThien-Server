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
        // Chờ script một lần trong Startup (vd. đặt lại thành chính) ghi xong DB rồi mới nạp trạng thái chiến.
        await Startup.Ready.Task.WaitAsync(stoppingToken);
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
        bool? wasOpen = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Chuyển giờ: hết giờ thì dừng mọi trận/di chuyển (người chơi vẫn ở lại scene); tới giờ thì ghép trận lại.
                var open = WarConfig.IsOpen(DateTime.UtcNow);
                if (wasOpen != open)
                {
                    if (!open) WarManager.CloseForToday();
                    else if (wasOpen != null) WarManager.OpenForToday();
                    wasOpen = open;
                }

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
