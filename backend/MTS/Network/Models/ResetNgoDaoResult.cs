using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class ResetNgoDaoResult : Result
{
    public int NewNgoDaoDiem { get; set; }
    public HashSet<NgoDao> NgoDaos { get; set; }
    public StatsSnapshot Stats { get; set; }
}