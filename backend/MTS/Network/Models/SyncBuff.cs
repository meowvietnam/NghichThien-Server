using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncBuff
    {
        public int CharId { get; set; }
        public Dictionary<int,int> Buffs { get; set; }
    }
}