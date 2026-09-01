using MemoryPack;
using MTS.Game.DP;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class GetDongPhuInfoResult : Result
    {
        public DongPhuInfo DongPhuInfo { get; set; }
    }
}
