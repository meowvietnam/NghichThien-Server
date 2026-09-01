using MemoryPack;
using MTS.Game.LuyenDan;

namespace MTS.Models;
[MemoryPackable]
public partial class Quality
{
    public string Name { get; private set; }
    public Tier Tier { get; private set; }
    public Color Color { get; private set; }
}
[MemoryPackable]
public partial class CoreResource
{
    public List<Quality> Qualities { get; private set; }
}