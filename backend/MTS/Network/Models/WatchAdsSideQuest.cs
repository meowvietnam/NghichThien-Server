using MemoryPack;
using MTS.Models;
using MTS.Network.Core;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class WatchAdsSideQuest : ShowAds
{
    [MemoryPackConstructor]
    public WatchAdsSideQuest()
    {
        
    }
    public WatchAdsSideQuest(string location, string buttonName, Client client) : base(location, buttonName, client)
    {
        
    }
    public override void OnReward()
    {
        var questHandler = Client.GetHandler<QuestHandler>();
        questHandler.ClaimSideQuestAds();
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
        return ShowAdsType.SideQuest;
    }
}