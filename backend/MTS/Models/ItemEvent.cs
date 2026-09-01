using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class ItemEvent : IEquatable<ItemEvent>
{
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public int Count { get; set; }
    public override int GetHashCode()
    {
        return Id;
    }
    public bool Equals(ItemEvent? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
}