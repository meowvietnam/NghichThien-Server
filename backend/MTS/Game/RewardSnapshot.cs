using MemoryPack;
using MTS.Models;

namespace MTS.Game
{
    [MemoryPackable]
    public partial class RewardSnapshot
    {
        public List<ItemRef> ItemRefs { get; set; }
        public int Money { get; set; }
        public int MoneyVip { get; set; }
    }
}