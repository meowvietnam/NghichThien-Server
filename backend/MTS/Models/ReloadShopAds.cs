using MemoryPack;
using MTS.Network;
using MTS.Network.Core;

namespace MTS.Models
{
    [MemoryPackable]
    public partial class ReloadShopAds : ShowAds
    {
        [MemoryPackIgnore]
        private EShopType shopType;
        [MemoryPackConstructor]
        public ReloadShopAds()
        {
            
        }
        public ReloadShopAds(string location, string buttonName, Client client, EShopType shopType) : base(location, buttonName, client)
        {
            this.shopType = shopType;
        }
        public override void OnReward()
        {
            var handler = Client.GetHandler<ShopHandler>();
            handler.SendReloadShop(shopType);
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