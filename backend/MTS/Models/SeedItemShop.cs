namespace MTS.Models;

public class SeedItemShop :  IEquatable<SeedItemShop>
{
    public int CharId { get; set; }
    public int PricePerSeed { get; set; }
    public int HerbId { get; set; }
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