using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class LinhNgoResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public ItemType Type { get; set; }
    public ushort Id { get; set; }
    public int RemainingMoney { get; set; }
    public TimeSpan NewTuoiTac { get; set; }
    public List<CamNgo> CamNgos { get; set; }
    public int NewNgoDaoDiem { get; set; }
}