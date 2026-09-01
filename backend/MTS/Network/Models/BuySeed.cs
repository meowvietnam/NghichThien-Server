using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class BuySeed
{
    public ushort HerbId { get; set; }
    public int Count { get; set; }
}