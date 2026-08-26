using MemoryPack;
using MTS.Game.DP;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class GetDongPhuInfoResult : Result
    {
        public DongPhuConfig Config { get; set; }
        public DongPhu DongPhu { get; set; }
    }
}
