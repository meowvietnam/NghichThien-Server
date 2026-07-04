using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class TuLuyenResult 
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int CurrentExp { get; set; }
    public int RemainingMoney { get; set; }
    public int NewTamCanh { get; set; }
    public int NewHp { get; set; }
    public int DeltaExp { get; set; }
    public int DeltaHp { get; set; }
}