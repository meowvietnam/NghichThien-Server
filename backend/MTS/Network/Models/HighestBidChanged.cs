using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class HighestBidChanged
    {
        public int PlayerId { get; set; }
        public int Amount { get; set; }
        public int MinAmount { get; set; }
        public int MaxAmount { get; set; }
    }
}