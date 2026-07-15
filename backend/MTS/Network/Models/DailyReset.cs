using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class DailyReset
{
    public int CurrentArenaCount { get; set; } = 0;
    public int CurrentCountSideQuest { get; set; } = 0;
}