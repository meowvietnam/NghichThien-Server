using MemoryPack;
using MTS.Models;

namespace MTS.Game.Auction
{
    [MemoryPackable]
    public partial class AuctionSession
    {
        public AuctionSessionStatus Status { get; set; }
        public string SessionTitle { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public ItemRef ItemRef { get; set; }
        public int DefaultPrice { get; set; }
        public int CurrentPrice { get; set; }
        public int MinPrice { get; set; }
        public int MaxPrice { get; set; }
        public HighestBidder HighestBidder { get; set; }
    }
}