using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class UpgradeDongPhuNotify
    {
        public DongPhuLevel NewLevel { get; set; }
    }
    [MemoryPackable]
    public partial class UpgradeDongPhuResult : Result
    {
        public DongPhuLevel NewLevel { get; set; }
        public int NewMoneyVip { get; set; }
    }
}
