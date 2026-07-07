using MemoryPack;
using MTS.Network;
using MTS.Network.Core;

namespace MTS.Models
{
    [MemoryPackable]
    public partial class MoneyVipAds : ShowAds
    {
        [MemoryPackConstructor]
        public MoneyVipAds()
        {
            
        }
        public MoneyVipAds(string location, string buttonName, Client client) : base(location, buttonName, client)
        {
        }
        public override void OnReward()
        {
            var handler = Client.GetHandler<AdsHandler>();
            handler.SendWatchAdsMoneyVipResult();
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
            return ShowAdsType.MoneyVip;
        }
    }
}