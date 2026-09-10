using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class ClanContributionRequest
{
    public int Times { get; set; }
}

[MemoryPackable]
public partial class ClanContributionResult : Result
{
    public ClanContribution? Contribution { get; set; }
    public int NewMoneyVip { get; set; }
}
