using MemoryPack;
using MTS.Models;

namespace MTS.Game.Rank;
[MemoryPackable]
public partial class RankLevel : IRank
{
    public RankType Type => RankType.RankLevel;
    public List<CharacterWrapper> Tops { get; private set; }
    public TimeSpan NextResetTime { get; private set; }
    public RankLevel(List<CharacterWrapper> tops, TimeSpan nextResetTime)
    {
        Tops = tops;
        NextResetTime = nextResetTime;
    }
}