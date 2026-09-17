using MTS.Game.Clan;

namespace MTS.Services;

// Gộp các lượt cộng Hồn Tinh từ farm quái (ClanManager.PendingSoulEssence, xem
// ClanManager.AddSoulEssenceFromMonsterKill) và ghi xuống DB theo chu kỳ, thay vì ghi
// mỗi lần giết quái — tránh ghi DB và tranh chấp gate liên tục khi nhiều thành viên
// trong cùng tông môn cùng farm.
public class ClanSoulEssenceFlushService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private readonly ILogger<ClanSoulEssenceFlushService> logger;

    public ClanSoulEssenceFlushService(ILogger<ClanSoulEssenceFlushService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            try
            {
                await ClanManager.FlushPendingSoulEssence();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(ClanSoulEssenceFlushService));
            }
        }
    }
}
