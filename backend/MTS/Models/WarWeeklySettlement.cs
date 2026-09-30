using System.ComponentModel.DataAnnotations;

namespace MTS.Models;

/// <summary>
/// Đánh dấu một tuần Tông môn chiến đã được chốt thưởng. Thư thưởng và bản ghi này được lưu cùng một lần
/// SaveChanges nên mỗi tuần chỉ phát thưởng đúng một lần, kể cả khi server restart.
/// </summary>
public class WarWeeklySettlement
{
    /// <summary>Mốc bắt đầu (UTC) của tuần đã chốt: 00:00 thứ Hai giờ Việt Nam.</summary>
    [Key]
    public DateTime WeekStart { get; set; }
    public DateTime SettledAt { get; set; }
    public int ClanCount { get; set; }
    public int MailCount { get; set; }
    public long TotalMoneyVip { get; set; }
}
