using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class CreateClanResult : Result
{
    public CreateClanBy By { get; set; }
    public int? NewMoneyVip { get; set; }
    public string? Limk { get; set; }
}