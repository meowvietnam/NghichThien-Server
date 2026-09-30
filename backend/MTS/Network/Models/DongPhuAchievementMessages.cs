using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class DongPhuAchievementTierView
{
    public int Id { get; set; }
    public long Target { get; set; }
    public bool IsClaimed { get; set; }
    public List<IEventReward> Rewards { get; set; } = new();
}

[MemoryPackable]
public partial class DongPhuAchievementView
{
    public string Id { get; set; } = string.Empty;
    /// <summary>DongPhuStat (6 = cấp động phủ: mục tiêu là DongPhuLevel).</summary>
    public byte Stat { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Mô tả mốc; "{target}" được client thay bằng mục tiêu của mốc.</summary>
    public string Description { get; set; } = string.Empty;
    public long Value { get; set; }
    public List<DongPhuAchievementTierView> Tiers { get; set; } = new();
}

[MemoryPackable]
public partial class GetDongPhuAchievementsResult : Result
{
    public List<DongPhuAchievementView> Achievements { get; set; } = new();
}

[MemoryPackable]
public partial class ClaimDongPhuAchievementRequest
{
    public string AchievementId { get; set; } = string.Empty;
    public int TierId { get; set; }
}

[MemoryPackable]
public partial class ClaimDongPhuAchievementResult : Result
{
    public string AchievementId { get; set; } = string.Empty;
    public int TierId { get; set; }
    public List<IEventReward> Rewards { get; set; } = new();
    /// <summary>Số mốc còn nhận được sau khi nhận (chấm đỏ).</summary>
    public int ClaimableCount { get; set; }
}

/// <summary>Server báo số mốc thành tựu đang nhận được (chấm đỏ trên nút Thành tựu).</summary>
[MemoryPackable]
public partial class DongPhuAchievementNotify
{
    public int ClaimableCount { get; set; }
}
