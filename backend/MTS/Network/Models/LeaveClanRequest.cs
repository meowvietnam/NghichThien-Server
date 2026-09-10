using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class LeaveClanRequest
{
}

[MemoryPackable]
public partial class LeaveClanResult : Result
{
    public int ClanId { get; set; }
}
