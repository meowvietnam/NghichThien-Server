using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class CreateOrderEventShop : IEventRequestData
{
    public ushort Id { get; set; }
}