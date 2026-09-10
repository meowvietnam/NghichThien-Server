using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class KickClanMemberRequest
{
    public int CharId { get; set; }
}

[MemoryPackable]
public partial class KickClanMemberResult : Result
{
    public int CharId { get; set; }
}
