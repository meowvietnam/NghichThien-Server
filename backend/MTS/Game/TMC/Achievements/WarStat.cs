namespace MTS.Game.TMC.Achievements;

/// <summary>
/// Chỉ số Tông môn chiến dùng cho thành tựu (đếm theo nhân vật trong một mùa 2 tuần, sang mùa mới về 0). Tên enum là khoá trong file cấu hình
/// <c>Data/war-achievements.json</c> và cột <c>WarAchievementProgresses.Stat</c> (lưu dạng số, KHÔNG đổi giá trị).
/// </summary>
public enum WarStat : byte
{
    /// <summary>Số điểm (node thường) trực tiếp chiếm: có mặt ở node lúc node về tay tông mình.</summary>
    CaptureNode = 0,
    /// <summary>Số thành (Stronghold) trực tiếp chiếm.</summary>
    CaptureStronghold = 1,
    /// <summary>Số trận được ghép (đánh người chơi, rối tông khác hay rối trung lập đều tính).</summary>
    Battle = 2,
    /// <summary>Số trận thắng (thắng rối cũng tính).</summary>
    Win = 3
}
