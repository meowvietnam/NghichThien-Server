using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class JoinAuctionResult : Result
    {
        public AuctionData AuctionData { get; set; }
    }
}