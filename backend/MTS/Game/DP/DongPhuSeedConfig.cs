using MemoryPack;
using MTS.Game.LuyenDan;

namespace MTS.Game.DP;

[MemoryPackable]
public partial class DongPhuSeedConfig
{
    public Tier Tier { get; set; }
    public TimeSpan GrowthDuration { get; set; }
}