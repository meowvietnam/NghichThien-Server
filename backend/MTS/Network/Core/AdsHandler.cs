using MTS.Game;
using MTS.Localization;
using MTS.Network.Models;

namespace MTS.Network.Core
{
    public class AdsHandler : IClientHandler
    {
        private Client client;

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
                var active = client.CharacterData.CurrentFreeAdsMoneyVipCount < GameConfig.TotalAdsMoneyVip;
                var config = new AdsMoneyVipConfig()
                {
                    MoneyVip = GameConfig.AdsMoneyVip,
                    Active = active,
                };
                client.Send(MessageTag.GetAdsMoneyVipConfigRequest, config);
            }
            else if (tag == MessageTag.WatchAdsMoneyVipRequest)
            {
                WatchAdsMoneyVipResult result = null;
                try
                {
                    var charData = client.CharacterData;
                    lock (charData)
                    {
                        var current = client.CharacterData.CurrentFreeAdsMoneyVipCount;
                        if (current >= GameConfig.TotalAdsMoneyVip)
                        {
                            throw new Exception(Strings.AdsMoneyVipLimitReached);
                        }
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
                    client.MemSend(MessageTag.WatchAdsMoneyVipRequest, result);
                }
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
            MessageTag.WatchAdsMoneyVipRequest
        };
        public MessageTag[] HandleTag => handler;
    }
}
