using MemoryPack;
using MTS.Game;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class GachaResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public RewardSnapshot Reward { get; set; }
        public int NewTienNgoc { get; set; }
    }
}
