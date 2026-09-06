using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
[MemoryPackUnion(0, typeof(MilestoneClaim))]
[MemoryPackUnion(1, typeof(CreateOrderRechargeEvent1))]
[MemoryPackUnion(2, typeof(CreateOrderEventShop))]
[MemoryPackUnion(3, typeof(BuyItemEventShop))]
public partial interface IEventRequestData
{
    
}