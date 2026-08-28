using MemoryPack;
using MTS.Network;
using MTS.Network.ClientState;

namespace MTS.Models;
[MemoryPackable]
public partial class ReloadDpShopAds : ShowAds
{
    [MemoryPackConstructor]
    public ReloadDpShopAds()
    {
        
    }
    public ReloadDpShopAds(string location, string buttonName, Client client) : base(location, buttonName, client)
    {
        
    }
    public override void OnReward()
    {
        var state = Client.ClientState as DongPhuState;
        if (state == null)
        {
            return;
        }

        state.ReloadShop();
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
        return ShowAdsType.ReloadDpShop;
    }
}