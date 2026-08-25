using MemoryPack;
using MTS.Models;

namespace MTS.Game.DP;
[MemoryPackable]
public partial class DongPhuLevelConfig
{
    public DongPhuLevel Level { get; set; }
    public ushort MaxWaterHelpPerDay  { get; set; }
    public ushort MyMaxWaterHelpPerDay { get; set; }
    public TimeSpan ReduceTime { get; set; }   
}