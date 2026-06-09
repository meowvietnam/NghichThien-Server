using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class TuLuyenResult 
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public bool HasLevelUpdated { get; set; }
    public int CurrentExp { get; set; }
    public int RemainingMoney { get; set; }
    public LevelUpdated LevelUpdated { get; set; }
}