using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class UseDanResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public StatsSnapshot? Stats { get; set; }
    public int? CurrentExp { get; set; }
    public HashSet<CamNgo>? CamNgos { get; set; }
    public int NgoDaoDiem { get; set; }
}