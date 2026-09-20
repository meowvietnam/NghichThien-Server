using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncEndTurnBoss
    {
        public int CharId { get; set; }
        public bool IsKietSuc { get; set; }
    }
}