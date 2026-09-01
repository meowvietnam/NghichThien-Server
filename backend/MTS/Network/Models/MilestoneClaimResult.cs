using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class MilestoneClaimResult : Result, IEventResultData
{
    public List<IEventReward> Rewards { get; set; }
    public bool IsSuccess()
    {
        return Success;
    }
}