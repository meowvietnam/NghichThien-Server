using MemoryPack;
using MTS.Network;

namespace MTS.Models
{
    public enum ShowAdsType
    {
        Arena,
        MoneyVip,
        ReloadTask,
        ReloadShop
    }
    [MemoryPackable]
    [MemoryPackUnion(0, typeof(MTS.Models.ArenaAds))]
    [MemoryPackUnion(1, typeof(MoneyVipAds))]
    [MemoryPackUnion(2, typeof(ReloadTaskAds))]
    [MemoryPackUnion(3, typeof(ReloadShopAds))]
    public abstract partial class ShowAds
    {
        [MemoryPackConstructor]
        public ShowAds()
        {
            
        }
        protected ShowAds(string location, string buttonName, Client client)
        {
            Location = location;
            ButtonName = buttonName;
            Client = client;
        }

        public string Location { get; set; }
        public string ButtonName { get; set; }
        [MemoryPackIgnore]
        public Client Client { get; private set; }
        [MemoryPackIgnore]
        public DateTime StartTime { get; private set; } = DateTime.UtcNow;

        public virtual bool Validate()
        {
            var deltaTime = DateTime.UtcNow - StartTime;
            return deltaTime.TotalSeconds >= 5;
        }
        public virtual void OnReward()
        {

        }
        public virtual void OnClose()
        {

        }

        public virtual void OnLoadFail()
        {
        }
        public abstract ShowAdsType GetAdsType();

    }
}
