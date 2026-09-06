using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncHealing
    {
        public int CharId { get; set; }
        public int Heal { get; set; }
    }
}