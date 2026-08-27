using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class SeedItemShop :  IEquatable<SeedItemShop>
{
    public int CharId { get; set; }
    public int PricePerSeed { get; set; }
    public ushort HerbId { get; set; }
    public int Count { get; set; }
    public override int GetHashCode()
    {
        return HerbId;
    }
    public bool Equals(SeedItemShop? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return HerbId == other.HerbId;
    }
}