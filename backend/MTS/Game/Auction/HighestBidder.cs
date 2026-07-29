using MemoryPack;

namespace MTS.Game.Auction
{
    [MemoryPackable]
    public partial class HighestBidder
    {
        public int CharId { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; }
    }
}