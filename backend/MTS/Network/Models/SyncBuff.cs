using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncBuff
    {
        public int CharId { get; set; }
        public int IdBuff { get; set; }
        public int NewBuff { get; set; }
    }
}