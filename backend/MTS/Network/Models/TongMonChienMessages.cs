using MemoryPack;
using MTS.Game.TMC;
using MTS.Models;

namespace MTS.Network.Models;

// Hợp đồng message Tông môn chiến. Client giữ bản sao y hệt thứ tự field tại
// Assets/Scripts/Network/Models/TongMonChien/TongMonChienMessages.cs (MemoryPack serialize theo thứ tự).

[MemoryPackable]
public partial class WarClanView
{
    public int ClanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int MainNodeId { get; set; }
    /// <summary>Số Thành Ấn: node Stronghold tông đang giữ (không gồm thành chính).</summary>
    public int StrongholdCount { get; set; }
    /// <summary>Số Địa Mạch: node thường tông đang giữ.</summary>
    public int NodeCount { get; set; }
}

[MemoryPackable]
public partial class WarNodeView
{
    public int NodeId { get; set; }
    public int? OwnerClanId { get; set; }
    public bool IsFighting { get; set; }
}

[MemoryPackable]
public partial class WarUnitRef
{
    public WarUnitKind Kind { get; set; }
    public int Id { get; set; }
}

[MemoryPackable]
public partial class WarUnitView
{
    public WarUnitKind Kind { get; set; }
    /// <summary>CharId với người chơi, PuppetId với rối.</summary>
    public int Id { get; set; }
    public int ClanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int NodeId { get; set; }
    public int Hp { get; set; }
    public int HpMax { get; set; }
    public float RegenPerMinute { get; set; }
    /// <summary>Mốc thời gian (UTC) của giá trị Hp; client ngoại suy hồi HP từ mốc này.</summary>
    public DateTime HpUpdatedAt { get; set; }
    public WarUnitState State { get; set; }
    /// <summary>Các node còn phải đi, không gồm NodeId.</summary>
    public int[] Route { get; set; } = [];
    /// <summary>Thời điểm (UTC) tới Route[0] khi State = Moving.</summary>
    public DateTime? NextArrivalAt { get; set; }
    /// <summary>Người chơi đang ở scene chiến (được tính là đối thủ). Rối luôn true.</summary>
    public bool IsActive { get; set; }
    public Gender Gender { get; set; }
    public Appearance? Appearance { get; set; }
}

[MemoryPackable]
public partial class WarGetSnapshotRequest
{
}

[MemoryPackable]
public partial class WarSnapshot : Result
{
    public long Version { get; set; }
    public float SecondsPerEdge { get; set; }
    public DateTime ServerTime { get; set; }
    public int MyCharId { get; set; }
    public int MyClanId { get; set; }
    public bool IsLeader { get; set; }
    public int MaxPuppetCount { get; set; }
    public int MaxPuppetLevel { get; set; }
    public WarClanView[] Clans { get; set; } = [];
    public WarNodeView[] Nodes { get; set; } = [];
    public WarUnitView[] Units { get; set; } = [];
    /// <summary>Tiên ngọc mỗi Thành Ấn khi chốt tuần.</summary>
    public int StrongholdMoneyVip { get; set; }
    /// <summary>Tiên ngọc mỗi Địa Mạch khi chốt tuần.</summary>
    public int NodeMoneyVip { get; set; }
    /// <summary>Thời điểm chốt thưởng tuần kế tiếp (UTC).</summary>
    public DateTime NextSettlementAt { get; set; }
}

[MemoryPackable]
public partial class WarDelta
{
    /// <summary>Tăng đúng 1 mỗi delta. Client thấy nhảy version thì tải lại snapshot.</summary>
    public long Version { get; set; }
    public DateTime ServerTime { get; set; }
    public WarClanView[] Clans { get; set; } = [];
    public WarNodeView[] Nodes { get; set; } = [];
    public WarUnitView[] Units { get; set; } = [];
    public WarUnitRef[] RemovedUnits { get; set; } = [];
}

[MemoryPackable]
public partial class WarMoveRequest
{
    public int TargetNodeId { get; set; }
}

[MemoryPackable]
public partial class WarMoveResult : Result
{
    public int[] Route { get; set; } = [];
}

[MemoryPackable]
public partial class WarDeployPuppetRequest
{
    public int PuppetId { get; set; }
    public int TargetNodeId { get; set; }
}

[MemoryPackable]
public partial class WarUseHpPillRequest
{
    /// <summary>0 = dùng cho bản thân; &gt; 0 = dùng cho rối (chỉ Leader).</summary>
    public int PuppetId { get; set; }
    public ushort DanId { get; set; }
    public ushort Count { get; set; }
}

[MemoryPackable]
public partial class WarUseHpPillResult : Result
{
    public int PuppetId { get; set; }
    public int Hp { get; set; }
    public int HpMax { get; set; }
    public ushort DanId { get; set; }
    public int RemainingCount { get; set; }
}

[MemoryPackable]
public partial class WarCreatePuppetRequest
{
}

[MemoryPackable]
public partial class WarUpgradePuppetRequest
{
    public int PuppetId { get; set; }
}

[MemoryPackable]
public partial class WarPuppetResult : Result
{
    public WarUnitView? Puppet { get; set; }
}

[MemoryPackable]
public partial class WarBattleMatched
{
    public long BattleId { get; set; }
    public int NodeId { get; set; }
    public bool IsPvp { get; set; }
    public bool EnemyIsPuppet { get; set; }
    public int EnemyPuppetId { get; set; }
    /// <summary>Snapshot chiến của mình: Hp/SinhMenh đã thay bằng HP chiến.</summary>
    public CharacterSnapshot Me { get; set; } = new();
    public CharacterSnapshot Enemy { get; set; } = new();
}

[MemoryPackable]
public partial class WarBattleReport
{
    public long BattleId { get; set; }
    public ResultCombat Result { get; set; } = new();
    /// <summary>HP còn lại của đối thủ (chỉ dùng cho trận với rối).</summary>
    public int EnemyHp { get; set; }
}

[MemoryPackable]
public partial class WarBattleEnded
{
    public long BattleId { get; set; }
    public WarSideResult Result { get; set; }
    public int Hp { get; set; }
    public int HpMax { get; set; }
    public int NodeId { get; set; }
    public string Message { get; set; } = string.Empty;
}

[MemoryPackable]
public partial class WarKicked
{
    public string Message { get; set; } = string.Empty;
}
