using MemoryPack;

namespace MTS.Game.Rank;

[MemoryPackable]
public partial class RankMonster : IRank
{
    public RankType Type => RankType.RankMonster;
    public TimeSpan NextResetTime { get; private set; }
    public List<MonsterRankEntity> Tops { get; private set; }
    public int MyPoints { get; private set; }
    public RankMonster(List<MonsterRankEntity> tops, TimeSpan nextResetTime, int myPoints)
    {
        Tops = tops;
        NextResetTime = nextResetTime;
        MyPoints = myPoints;
    }
}