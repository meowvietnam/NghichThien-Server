using MemoryPack;
using MTS.Game.LuyenKhi;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class SetClanInheritanceRequest
{
    public ClanInheritanceTrigger Trigger { get; set; }
    public int SlotIndex { get; set; }
    // null xóa hiệu ứng đang có trong ô.
    public EffectType? EffectType { get; set; }
}

[MemoryPackable]
public partial class SetClanInheritanceResult : Result
{
    public ClanInheritanceChanged Change { get; set; } = new();
    public int NewMoneyVip { get; set; }
    public long ClanFunds { get; set; }
    public int Cost { get; set; }
}

[MemoryPackable]
public partial class ClanInheritanceChanged
{
    public int ClanId { get; set; }
    public ClanInheritanceTrigger Trigger { get; set; }
    public int SlotIndex { get; set; }
    public ClanInheritanceEffect? Effect { get; set; }
}

[MemoryPackable]
public partial class ClanExpChanged
{
    public int ClanId { get; set; }
    public long Exp { get; set; }
}

[MemoryPackable]
public partial class ClanFundsChanged
{
    public int ClanId { get; set; }
    public long Funds { get; set; }
}

[MemoryPackable]
public partial class ClanLevelChanged
{
    public int ClanId { get; set; }
    public int Level { get; set; }
    public long Exp { get; set; }
    public int MaxMembers { get; set; }
    public int InheritanceSlotsPerColumn { get; set; }
    public ClanInheritanceEffect[] Effects { get; set; } = [];
}

[MemoryPackable]
public partial class UpgradeClanWithFundsRequest
{
    public long Funds { get; set; }
}

[MemoryPackable]
public partial class UpgradeClanWithFundsResult : Result
{
    public long FundsSpent { get; set; }
    public long AddedExp { get; set; }
    public long ClanFunds { get; set; }
    public long Exp { get; set; }
    public int Level { get; set; }
    public int MaxMembers { get; set; }
    public int InheritanceSlotsPerColumn { get; set; }
    public ClanInheritanceEffect[] Effects { get; set; } = [];
}

[MemoryPackable]
public partial class ClanHonChanged
{
    public int ClanId { get; set; }
    public long Hon { get; set; }
    public long MaxHon { get; set; }
}

[MemoryPackable]
public partial class LuyenHonRequest
{
    public long Amount { get; set; }
}

[MemoryPackable]
public partial class LuyenHonResult : Result
{
    public long AddedHon { get; set; }
    public long Hon { get; set; }
    public long MaxHon { get; set; }
}

[MemoryPackable]
public partial class UpgradeClanMaxHonRequest
{
    public long Funds { get; set; }
}

[MemoryPackable]
public partial class UpgradeClanMaxHonResult : Result
{
    public long FundsSpent { get; set; }
    public long AddedMaxHon { get; set; }
    public long ClanFunds { get; set; }
    public long Hon { get; set; }
    public long MaxHon { get; set; }
}
