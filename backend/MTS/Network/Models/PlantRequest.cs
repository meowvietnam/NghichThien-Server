using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class PlantRequest
    {
        public ushort SlotIndex { get; set; }
        public ushort HerbId { get; set; }
    }
}
