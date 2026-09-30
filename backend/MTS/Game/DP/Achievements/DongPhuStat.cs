namespace MTS.Game.DP.Achievements;

/// <summary>
/// Chỉ số Động phủ dùng cho thành tựu. Tên enum là khoá dùng trong file cấu hình
/// <c>Data/dong-phu-achievements.json</c> và cột <c>DongPhuAchievementProgresses.Stat</c> (lưu dạng số, KHÔNG đổi giá trị).
/// </summary>
public enum DongPhuStat : byte
{
    /// <summary>Số lần tưới nước hộ động phủ người khác.</summary>
    WaterHelpGiven = 0,
    /// <summary>Số lần động phủ của mình được người khác tưới hộ.</summary>
    WaterHelpReceived = 1,
    /// <summary>Số lần like động phủ người khác.</summary>
    LikeGiven = 2,
    /// <summary>Số lượt like động phủ của mình nhận được.</summary>
    LikeReceived = 3,
    /// <summary>Số lần gieo hạt.</summary>
    Plant = 4,
    /// <summary>Số lần thu hoạch ô đất.</summary>
    Harvest = 5,
    /// <summary>Cấp động phủ hiện tại (DongPhuLevel: 0 Phàm, 1 Hoàng, 2 Huyền, 3 Địa, 4 Thiên, 5 Thần). Không lưu, đọc trực tiếp.</summary>
    Level = 6,
    /// <summary>Tổng số hạt giống đã mua.</summary>
    SeedBought = 7,
    /// <summary>Số lần thăm động phủ người khác (mỗi động phủ tối đa 1 lần/ngày).</summary>
    Visit = 8
}
