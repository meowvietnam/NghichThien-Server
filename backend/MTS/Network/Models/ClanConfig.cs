using System.Collections.Generic;
using MemoryPack;
using MTS.Game;
using MTS.Models;
using MTS.Game.LuyenKhi;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class ClanConfig
{
    public int CreateClanMoneyVipPrice { get; set; }
    public int CreateClanVndPrice { get; set; }
    public int CreateClanVndBonusMoneyVip { get; set; }
    public int CreateMaxMember { get; set; }
    public int MemberSlotLevelInterval { get; set; } = 1;
    public int MemberSlotsPerInterval { get; set; } = 5;
    public int ContributionMoneyVipPerTime { get; set; }
    public int MaxContributionTimesPerRequest { get; set; }
    public int MaxShopQuantityPerRequest { get; set; } = 100;
    public int MaxShopItemsAddedPerDay { get; set; } = 3;
    public Dictionary<ClanRole, ClanPermission> RolePermissions { get; set; } = new();
    public int BaseInheritanceSlots { get; set; } = 1;
    public int InheritanceSlotsPerLevel { get; set; } = 1;
    public int MaxInheritanceSlotsPerColumn { get; set; } = 10;
    public int ChangeInheritanceMoneyVipCost { get; set; } = 1_000;
    public int InheritanceBaseValue { get; set; } = 1;
    public int InheritanceValuePerClanLevel { get; set; } = 1;
    public EffectType[] InheritanceBuffWhitelist { get; set; } = [];
    public long[] ClanLevelRequiredTotalExp { get; set; } = [];
    public bool EnableCreateClanByVnd => GameConfig.EnableWebPay;
}
