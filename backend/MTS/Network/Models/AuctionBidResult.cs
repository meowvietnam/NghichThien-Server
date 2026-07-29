using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class AuctionBidResult : Result
    {
        public int NewMoneyVip { get; set; }
        public int Amount { get; set; }
    }
}