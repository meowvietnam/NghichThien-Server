using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class CreateNewAuctionSession
{
    public int Price { get; set; }
    public ItemRef Item { get; set; }
}