using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ReplaceAuctionPlayer
    {
        public int PlayerId { get; set; }
        public PlayerAuctionModel Player { get; set; }
    }
}