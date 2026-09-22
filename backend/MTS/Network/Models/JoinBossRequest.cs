using MemoryPack;

namespace MTS.Network.Models
{
[MemoryPackable]
public partial class JoinBossRequest
{
    public long SpawnId { get; set; }
    public int NodeId { get; set; }
}
}
