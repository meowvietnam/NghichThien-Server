using MemoryPack;
using MTS.Game.DP;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class HarvestSlotNotify
    {
        public ushort SlotIndex { get; set; }
    }
    [MemoryPackable]
    public partial class HarvestSlotResult : Result
    {
        public ushort SlotIndex { get; set; }
        public List<IDongPhuReward> Rewards { get; set; }
    }
}
