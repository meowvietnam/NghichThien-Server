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

[MemoryPackable]
public partial class GetClanMapNodesRequest
{
}

[MemoryPackable]
public partial class GetClanMapNodesResult : Result
{
    public ClanMapNode[] Nodes { get; set; } = [];
}

[MemoryPackable]
public partial class ClanMapNode
{
    public int ClanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NodeId { get; set; }
    public string? ImageUrl { get; set; }
}

[MemoryPackable]
public partial class ClanMapNodeChanged
{
    public ClanMapNode Node { get; set; } = new();
}
