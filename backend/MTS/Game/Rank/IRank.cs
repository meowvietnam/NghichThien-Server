using MemoryPack;

namespace MTS.Game.Rank;
[MemoryPackable]
[MemoryPackUnion(0, typeof(RankLevel))]
[MemoryPackUnion(1, typeof(RankTongNap))]
[MemoryPackUnion(2, typeof(RankTongTieu))]
[MemoryPackUnion(3, typeof(RankArena))]
public partial interface IRank
{
    RankType Type { get; }
    TimeSpan NextResetTime { get; }
}