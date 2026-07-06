using MemoryPack;
using MTS.Game.Rank;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class GetRankResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public IRank Rank { get; set; }
    }
}