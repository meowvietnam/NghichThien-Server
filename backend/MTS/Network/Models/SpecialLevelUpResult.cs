using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class SpecialLevelUpResult : Result
{
    public StatsSnapshot StatsSnapshot { get; set; }
    public List<ItemRef> BuffItems { get; set; }
    public int CurrentExp { get; set; }
    public HashSet<CamNgo> CamNgos { get; set; }
}