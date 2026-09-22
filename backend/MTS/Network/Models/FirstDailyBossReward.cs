using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class FirstDailyBossReward
{
    public List<IMailReward> Rewards { get; set; } = new();
}