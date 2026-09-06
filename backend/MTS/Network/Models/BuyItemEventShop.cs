using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class BuyItemEventShop : IEventRequestData
{
    public ushort Id { get; set; }
}