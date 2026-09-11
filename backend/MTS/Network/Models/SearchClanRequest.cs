using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class SearchClanRequest
{
    public string Name { get; set; } = string.Empty;
    public int Limit { get; set; } = 20;
}

[MemoryPackable]
public partial class SearchClanResult : Result
{
    public Clan[] Clans { get; set; } = [];
}
