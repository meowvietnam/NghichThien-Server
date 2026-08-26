using MemoryPack;
using MTS.Models;

namespace MTS.Game.DP;
[MemoryPackable]
public partial class DongPhuLevelConfig
{
    public string Name { get; set; }
    public DongPhuLevel Level { get; set; }
    public ushort SlotMaxWaterHelpPerDay  { get; set; }
    public ushort MyMaxWaterHelpPerDay { get; set; }
    public TimeSpan ReduceTime { get; set; }  
    public int RequireMoneyVip { get; set; }
}