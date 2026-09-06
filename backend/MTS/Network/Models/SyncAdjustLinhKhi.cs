using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncAdjustLinhKhi
    {
        public int CharId { get; set; }
        public int Kim { get; set; }
        public int Moc { get; set; }
        public int Thuy { get; set; }
        public int Hoa { get; set; }
        public int Tho { get; set; }
        public int Ma { get; set; }
    }
}