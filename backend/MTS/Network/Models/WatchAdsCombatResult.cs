using MemoryPack;
using MTS.Game;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class WatchAdsCombatResult : Result
{
    public RewardSnapshot Reward { get; set; }
}