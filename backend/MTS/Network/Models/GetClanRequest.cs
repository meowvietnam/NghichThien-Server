using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class GetClanRequest
{
}

[MemoryPackable]
public partial class GetClanResult : Result
{
    public Clan? Clan { get; set; }
    public ClanConfig Config { get; set; } = new();
}
