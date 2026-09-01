using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class ClaimMonsterKillRewardResult : Result
{
    public long Id { get; set; }
    public List<IEventReward> Rewards { get; set; }
}