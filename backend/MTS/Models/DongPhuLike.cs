using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class DongPhuLike : IEquatable<DongPhuLike>
{
    public int OwnerCharId { get; set; }
    public int OtherCharId { get; set; }

    public bool Equals(DongPhuLike? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return OwnerCharId == other.OwnerCharId && OtherCharId == other.OtherCharId;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(OwnerCharId, OtherCharId);
    }
}