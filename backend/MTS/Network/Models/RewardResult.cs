using MemoryPack;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class RewardResult 
{
    public List<long> Mails { get; set; }
    public List<IMailReward> Rewards { get; set; }
}