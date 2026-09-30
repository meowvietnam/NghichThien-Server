using MTS.Game.DP.Achievements;

namespace MTS.Models;

/// <summary>Tiến độ một chỉ số thành tựu Động phủ của nhân vật (đếm trọn đời) và các mốc đã nhận.</summary>
public class DongPhuAchievementProgress
{
    public int CharId { get; set; }
    public DongPhuStat Stat { get; set; }
    public long Value { get; set; }
    /// <summary>Các mốc đã nhận thưởng, dạng "achievementId:tierId;..." (ví dụ "water_help:1;water_help:2").</summary>
    public string ClaimedTiers { get; set; } = string.Empty;
}
