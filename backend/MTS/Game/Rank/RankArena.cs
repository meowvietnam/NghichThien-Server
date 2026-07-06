using MemoryPack;

namespace MTS.Game.Rank;
[MemoryPackable]
public partial class RankArena : IRank
{
    public RankType Type => RankType.RankArena;
    public List<CharacterWrapper> Tops { get; private set; }
    public TimeSpan NextResetTime { get; private set; }
    public Dictionary<ETypeArenaRank, int> TypeRankArenaWithValue { get; private set; }
    public Dictionary<ETypeArenaRank, int> TypeRankArenaWithReward { get; private set; }
    public RankArena(List<CharacterWrapper> tops, TimeSpan nextResetTime,  Dictionary<ETypeArenaRank, int> typeRankArenaWithValue, Dictionary<ETypeArenaRank, int> typeRankArenaWithReward)
    {
        Tops = tops;
        NextResetTime = nextResetTime;
        TypeRankArenaWithValue = typeRankArenaWithValue;
        TypeRankArenaWithReward = typeRankArenaWithReward;
    }
}