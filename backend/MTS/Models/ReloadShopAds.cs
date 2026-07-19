using MemoryPack;
using MTS.Network;
using MTS.Network.Core;
using MTS.Network.Models;

namespace MTS.Models
{
    [MemoryPackable]
    public partial class ReloadShopAds : ShowAds
    {
        [MemoryPackIgnore]
        private ReloadShopRequest request;
        [MemoryPackConstructor]
        public ReloadShopAds()
        {
            
        }
        public ReloadShopAds(string location, string buttonName, Client client, ReloadShopRequest request) : base(location, buttonName, client)
        {
            this.request = request;
        }
        public override void OnReward()
        {
            var handler = Client.GetHandler<ShopHandler>();
            handler.SendReloadShop(request);
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
            return ShowAdsType.ReloadShop;
        }
    }
}