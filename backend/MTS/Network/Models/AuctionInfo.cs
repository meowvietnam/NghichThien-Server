using MemoryPack;
using MTS.Game.Auction;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class AuctionInfo
    {
        public string Content { get; set; }
        public bool IsOpen { get; set; }
        public AuctionSession[] AuctionSessions { get; set; }
    }
}