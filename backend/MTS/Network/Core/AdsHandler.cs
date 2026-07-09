using MTS.Game;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.Core
{
    public class AdsHandler : IClientHandler
    {
        private Client client;
        private readonly object adsLock = new();
        public Dictionary<ShowAdsType, ShowAds> Ads { get; private set; } = new();

        public AdsHandler(Client client)
        {
            this.client = client;
        }

        public void OnEnter()
        {
        }

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            if (tag == MessageTag.GetAdsMoneyVipConfigRequest)
            {
                bool active;
                lock (client.CharacterLock)
                {
                    active = client.CharacterData.CurrentFreeAdsMoneyVipCount < GameConfig.TotalAdsMoneyVip;
                }
                var config = new AdsMoneyVipConfig()
                {
                    MoneyVip = GameConfig.AdsMoneyVip,
                    Active = active,
                };
                client.Send(MessageTag.GetAdsMoneyVipConfigRequest, config);
            }
            else if (tag == MessageTag.WatchAdsMoneyVip)
            {
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        var current = charData.CurrentFreeAdsMoneyVipCount;
                        if (current >= GameConfig.TotalAdsMoneyVip)
                        {
                            throw new Exception(Strings.AdsMoneyVipLimitReached);
                        }

                        if (charData.IsNoAds)
                        {
                            SendWatchAdsMoneyVipResult();
                        }
                        else
                        {
                            ShowAds ads = new MoneyVipAds("MoneyVipAds", "Xem quảng cáo để nhận tiên ngọc", client);
                            SendAds(ads);
                        }
                    }
                }
                catch (Exception e)
                {
                    var result = new WatchAdsMoneyVipResult()
                    {
                        Success = false,
                        Message = e.Message,
                    };
                    client.MemSend(MessageTag.WatchAdsMoneyVipResult, result);
                }
            }
            else if (tag == MessageTag.CompleteAds)
            {
                var type = reader.Read<ShowAdsType>();
                ShowAds ads;
                lock (adsLock)
                {
                    Ads.Remove(type, out ads);
                }
                if (ads != null)
                {
                    if (ads.Validate())
                    {
                        ads.OnReward();
                    }
                }
            }
            else if (tag == MessageTag.CloseAds)
            {
                var type = reader.Read<ShowAdsType>();
                ShowAds ads;
                lock (adsLock)
                {
                    Ads.Remove(type, out ads);
                }
                if (ads != null)
                {
                    ads.OnClose();
                }
            }
            else if (tag == MessageTag.LoadAdsFail)
            {
                var type = reader.Read<ShowAdsType>();
                ShowAds ads;
                lock (adsLock)
                {
                    Ads.Remove(type, out ads);
                }
                if (ads != null)
                {
                    ads.OnLoadFail();
                }
            }
        }

        public void SendWatchAdsMoneyVipResult()
        {
            WatchAdsMoneyVipResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    var current = charData.CurrentFreeAdsMoneyVipCount;
                    current++;
                    var newMoneyVip = charData.MoneyVip + GameConfig.AdsMoneyVip;
                    charData.SetMoneyVip(newMoneyVip);
                    charData.SetCurrentFreeAdsMoneyVipCount(current);
                    result = new WatchAdsMoneyVipResult()
                    {
                        Success = true,
                        MoneyVip = GameConfig.AdsMoneyVip,
                        Count = current,
                    };
                    if (current >= GameConfig.TotalAdsMoneyVip)
                    {
                        client.Send(MessageTag.ActiveAdsMoneyVip, false);
                    }
                }
            }
            catch (Exception e)
            {
                result = new WatchAdsMoneyVipResult()
                {
                    Success = false,
                    Message = e.Message,
                };
            }
            finally
            {
                client.MemSend(MessageTag.WatchAdsMoneyVipResult, result);
            }
        }

        public void OnLeave()
        {
        }

        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
        }

        private static MessageTag[] handler =
        {
            MessageTag.GetAdsMoneyVipConfigRequest,
            MessageTag.WatchAdsMoneyVip,
            MessageTag.CompleteAds,
            MessageTag.CloseAds,
            MessageTag.LoadAdsFail,
        };

        public MessageTag[] HandleTag => handler;

        public void SendAds(ShowAds ads)
        {
            lock (adsLock)
            {
                Ads[ads.GetAdsType()] = ads;
            }
            client.MemSend(MessageTag.WatchAds, ads);
        }
    }
}
