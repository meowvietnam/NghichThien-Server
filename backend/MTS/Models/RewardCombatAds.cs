
using MemoryPack;
using MTS.Network;
using MTS.Network.Core;

namespace MTS.Models;
[MemoryPackable]
public partial class RewardCombatAds : ShowAds
{
    [MemoryPackConstructor]
    public RewardCombatAds()
    {
        
    }
    public RewardCombatAds(string location, string buttonName, Client client) : base(location, buttonName, client)
    {
        
    }

    public override void OnReward()
    {
        var combatHandler = Client.GetHandler<CombatHandler>();
        combatHandler.ClaimCombatAds();
    }

    public override void OnClose()
    {
    }

    public override void OnLoadFail()
    {
        OnReward();
    }

    public override ShowAdsType GetAdsType()
    {
        return ShowAdsType.RewardCombat;
    }
}