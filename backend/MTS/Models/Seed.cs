using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class Seed : IEquatable<Seed>
{
    public int CharId { get; set; }
    public ushort HerbId { get; set; }
    public int Count { get; set; }
    public override int GetHashCode()
    {
        return HerbId;
    }
    public bool Equals(Seed? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return HerbId == other.HerbId;
    }

    public Seed Clone()
    {
        return new Seed()
        {
            CharId = CharId,
            HerbId = HerbId,
            Count = Count,
        };
    }
}