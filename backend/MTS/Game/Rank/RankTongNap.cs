using MemoryPack;

namespace MTS.Game.Rank;
[MemoryPackable]
public partial class RankTongNap : IRank
{
    public RankType Type => RankType.RankTongNap;
    public List<RechargeRankEntry> Tops { get; private set; }
    
    public TimeSpan NextResetTime { get; private set; }
    public long MySpent { get; private set; }
    
    public RankTongNap(List<RechargeRankEntry> tops, TimeSpan nextResetTime, long mySpent)
    {
        Tops = tops;
        NextResetTime = nextResetTime;
        MySpent = mySpent;
    }

}