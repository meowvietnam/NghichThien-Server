using MemoryPack;
using MTS.Game;

namespace MTS.Network.Models
{
    public enum MatchMakingState
    {
        Free,
        Ads,
        NotFree
    }
    [MemoryPackable]
    public partial class LoadRankResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<ETypeArenaRank, int> TypeRankArenaWithValue { get; set; }
        public Dictionary<ETypeArenaRank, int> TypeRankArenaWithReward { get; set; }
        public TimeSpan NextReload { get; set; }
        public MatchMakingState MatchMakingState { get; set; }
        public int Cost { get; set; }
    }
}