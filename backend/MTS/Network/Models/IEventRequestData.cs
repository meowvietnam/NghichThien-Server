using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
[MemoryPackUnion(0, typeof(MilestoneClaim))]
[MemoryPackUnion(1, typeof(CreateOrderRechargeEvent1))]
public partial interface IEventRequestData
{
    
}