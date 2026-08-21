namespace MTS.Models;

public class Seed : IEquatable<Seed>
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
}