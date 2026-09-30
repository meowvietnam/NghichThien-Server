namespace MTS.Models;

public enum WarSettlementKind : byte
{
    /// <summary>Phát tiên ngọc hằng ngày theo lãnh thổ.</summary>
    Daily = 0,
    /// <summary>Kết thúc mùa: làm mới đất đã chiếm.</summary>
    SeasonReset = 1
}

/// <summary>
/// Đánh dấu một mốc Tông môn chiến đã xử lý (chốt thưởng ngày hoặc làm mới mùa). Thư thưởng và bản ghi này được lưu
/// cùng một lần SaveChanges nên mỗi mốc chỉ xử lý đúng một lần, kể cả khi server restart.
/// </summary>
public class WarSettlement
{
    public WarSettlementKind Kind { get; set; }
    /// <summary>Mốc bắt đầu (UTC) của ngày đã chốt / mùa mới đã bắt đầu, theo giờ Việt Nam.</summary>
    public DateTime PeriodStart { get; set; }
    public DateTime SettledAt { get; set; }
    public int ClanCount { get; set; }
    public int MailCount { get; set; }
    public long TotalMoneyVip { get; set; }
}
