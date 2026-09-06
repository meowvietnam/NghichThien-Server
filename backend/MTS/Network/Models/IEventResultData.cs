using MemoryPack;

namespace MTS.Network.Models;


[MemoryPackable]
[MemoryPackUnion(0, typeof(MilestoneClaimResult))]
[MemoryPackUnion(1, typeof(RechargeEvent1Result))]
[MemoryPackUnion(2, typeof(CreateOrderEventShopResult))]
[MemoryPackUnion(3, typeof(BuyItemEventShopResult))]
public partial interface IEventResultData
{
    bool IsSuccess();
}