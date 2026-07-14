using MemoryPack;
using MTS.Models;
using MTS.Network.Models;

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