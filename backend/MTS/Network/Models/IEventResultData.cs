using MemoryPack;

namespace MTS.Network.Models;


[MemoryPackable]
[MemoryPackUnion(0, typeof(MilestoneClaimResult))]
[MemoryPackUnion(1, typeof(RechargeEvent1Result))]
public partial interface IEventResultData
{
    bool IsSuccess();
}