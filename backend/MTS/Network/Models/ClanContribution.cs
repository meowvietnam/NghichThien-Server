using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class ClanContribution
{
    public int ClanId { get; set; }
    public int CharId { get; set; }
    public int Times { get; set; }
    public int FreeTimes { get; set; }
    public int PaidTimes { get; set; }
    public long Amount { get; set; }
    public long ClanFunds { get; set; }
    public long MemberContribPoints { get; set; }
    public System.DateTime LastFreeContributionAt { get; set; }
    public int FreeContributionTimesRemaining { get; set; }
    public long MemberTotalContribPoints { get; set; }
}
