using MemoryPack;
using MTS.Game.BaseSkill;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class RechargeMilestone1
{
    public ushort Id { get; set; }
    public ETypeSkill Type { get; set; }
    public string Name { get; set; }
    public int Amount { get; set; }
    public List<IEventReward> Rewards { get; set; }
    public bool IsClaim { get; set; }

    public List<IMailReward> GetRewards()
    {
        return Rewards.SelectMany(x => x.GetMailReward()).ToList();
    }
}