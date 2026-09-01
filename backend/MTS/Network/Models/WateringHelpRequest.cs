using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class WateringHelpRequest
{
    public ushort SlotIndex { get; set; }
}