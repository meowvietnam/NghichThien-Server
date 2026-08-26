using MemoryPack;
using MTS.Game.DP;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class WateringHelpNotify
    {
        public ushort SlotIndex { get; set; }
        public TimeSpan ReduceTime { get; set; }
    }
    [MemoryPackable]
    public partial class WateringHelpResult : Result
    {
        public ushort SlotIndex { get; set; }
        public TimeSpan ReduceTime { get; set; }
        public List<IDongPhuReward> Rewards { get; set; }
    }
}
