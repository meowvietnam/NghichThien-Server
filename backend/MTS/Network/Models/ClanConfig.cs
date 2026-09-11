using System.Collections.Generic;
using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class ClanConfig
{
    public int CreateClanMoneyVipPrice { get; set; }
    public int CreateClanVndPrice { get; set; }
    public int CreateMaxMember { get; set; }
    public int ContributionMoneyVipPerTime { get; set; }
    public int MaxContributionTimesPerRequest { get; set; }
    public int MaxShopQuantityPerRequest { get; set; } = 100;
    public int MaxShopItemsAddedPerDay { get; set; } = 100;
    public Dictionary<ClanRole, ClanPermission> RolePermissions { get; set; } = new();
}
