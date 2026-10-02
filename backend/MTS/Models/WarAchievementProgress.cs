using MTS.Game.TMC.Achievements;

namespace MTS.Models;

/// <summary>Tiến độ một chỉ số thành tựu Tông môn chiến của nhân vật (đếm trọn đời) và các mốc đã nhận.</summary>
public class WarAchievementProgress
{
    public int CharId { get; set; }
    public WarStat Stat { get; set; }
    public long Value { get; set; }
    /// <summary>Các mốc đã nhận thưởng, dạng "achievementId:tierId;...".</summary>
    public string ClaimedTiers { get; set; } = string.Empty;
}
