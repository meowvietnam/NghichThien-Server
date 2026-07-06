using MemoryPack;

namespace MTS.Game.Rank;
[MemoryPackable]
public partial class RankTongTieu : IRank
{
    public RankType Type => RankType.RankTongTieu;
    public List<MoneyVipSpendingRankEntry> Tops { get; private set; }
    public TimeSpan NextResetTime { get; private set; }
    public long MySpent { get; private set; }

    public RankTongTieu(List<MoneyVipSpendingRankEntry> tops, TimeSpan nextResetTime, long mySpent)
    {
        Tops = tops;
        NextResetTime = nextResetTime;
        MySpent = mySpent;
    }

}