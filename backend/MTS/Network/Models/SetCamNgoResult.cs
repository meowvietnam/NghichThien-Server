using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class SetCamNgoResult
{
    public CamNgo CamNgo { get; set; }
    public int NewNgoDaoDiem { get; set; }
}