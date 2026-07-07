using MemoryPack;
using MTS.Network;

namespace MTS.Models
{
    [MemoryPackable]
    public partial class ReloadTaskAds : ShowAds
    {
        [MemoryPackIgnore]
        private EDailyQuest eDailyQuest;
        [MemoryPackConstructor]
        public ReloadTaskAds()
        {
            
        }
        public ReloadTaskAds(string location, string buttonName, Client client, EDailyQuest eDailyQuest) : base(location, buttonName, client)
        {
            this.eDailyQuest = eDailyQuest;
        }

        public override void OnReward()
        {
            
        }

        public override void OnClose()
        {
        }

        public override void OnLoadFail()
        {
        }

        public override ShowAdsType GetAdsType()
        {
            return ShowAdsType.ReloadTask;
        }
    }
}