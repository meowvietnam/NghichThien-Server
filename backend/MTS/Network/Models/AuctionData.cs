using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class AuctionData
    {
        public CurrentAuctionItem CurrentItem { get; set; }
        public PlayerAuctionModel[] Players { get; set; }
        public PlayerAuctionModel MyPlayer { get; set; }
        public int MyMoneyVip { get; set; }
    }
}