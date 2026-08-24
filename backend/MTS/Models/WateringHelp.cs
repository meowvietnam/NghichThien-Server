using MemoryPack;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class WateringHelp : ICustomEquals<WateringHelp>
{
    public int CharId { get; set; }
    public string Name { get; set; }
    public bool CustomEquals(WateringHelp other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return CharId == other.CharId && Name == other.Name;
    }
}