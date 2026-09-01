using MemoryPack;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Game
{
    [MemoryPackable]
    public partial class RewardSnapshot
    {
        public RewardItems RewardItems;
        public List<ItemRef> ItemRefs;
        public List<ItemEvent> itemEvents;
        public int Money;
        public int MoneyVip;
    }
}