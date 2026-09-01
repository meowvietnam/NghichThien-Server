using MemoryPack;
using MTS.Game.LuyenDan;
using MTS.Game.LuyenKhi;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class RandomLuyenKhiRequest
{
    public ArtifactType SelectedType { get; set; }
    public HashSet<Tier> Tiers { get; set; }
    public int Count { get; set; }
}