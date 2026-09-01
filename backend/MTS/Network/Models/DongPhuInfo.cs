using MemoryPack;
using MTS.Game.DP;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class DongPhuInfo
    {
        public DongPhuConfig Config { get; set; }
        public DongPhu DongPhu { get; set; }
        public DongPhuLevel MyLevel { get; set; }
        public int CurrentWateringHelpCount { get; set; }
        public bool CanLike { get; set; }
    }
}
