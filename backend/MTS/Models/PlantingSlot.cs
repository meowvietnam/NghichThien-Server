using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class PlantingSlot : IEquatable<PlantingSlot>
{
    public int CharId { get; set; }
    public ushort SlotIndex { get; set; }
    public bool IsLocked { get; set; } = true;
    public DongPhuLevel UnlockLevel { get; set; }
    public bool Equals(PlantingSlot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SlotIndex == other.SlotIndex;
    }

    public string GetName()
    {
        return $"ô thứ {SlotIndex + 1}";
    }
    public override int GetHashCode()
    {
        return SlotIndex;
    }
}