using MemoryPack;
using MTS.Models;

namespace MTS.Game
{
    [MemoryPackable]
    public partial class RewardSnapshot
    {
        public List<ItemRef> ItemRefs = new();
        public int Money = 0;
        public int MoneyVip = 0;
    }
}