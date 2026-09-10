using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class ClanConfig
{
    public int CreateClanMoneyVipPrice { get; set; }
    public int CreateClanVndPrice { get; set; }
    public int CreateMaxMember { get; set; }
    public int ContributionMoneyVipPerTime { get; set; }
    public int MaxContributionTimesPerRequest { get; set; }
}
