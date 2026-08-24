using MemoryPack;

namespace MTS.Models;

public class PlantingSlot : IEquatable<PlantingSlot>
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
    public override int GetHashCode()
    {
        return SlotIndex;
    }
}