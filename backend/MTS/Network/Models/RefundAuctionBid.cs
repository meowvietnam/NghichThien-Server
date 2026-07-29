using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class RefundAuctionBid
    {
        public int RefundMoney { get; set; }
        public int NewMoneyVip { get; set; }
    }
}