using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial struct UseDanRequest
{
    public ushort Id { get; private set; }
    public ushort Count { get; private set; }
}