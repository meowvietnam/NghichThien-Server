using MemoryPack;
using MTS.Network;
using MTS.Network.Core;

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
            var handler = Client.GetHandler<QuestHandler>();
            handler.SendReloadDailyQuest(eDailyQuest);
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
            return ShowAdsType.ReloadTask;
        }
    }
}