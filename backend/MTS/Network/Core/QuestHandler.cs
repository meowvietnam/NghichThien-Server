using MTS.Exceptions;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core
{
    public class QuestCompleted
    {
        public bool HasClaimAds { get; set; }
        public int Money { get; set; }
        public int MoneyVip { get; set; }
    }
    public class QuestHandler : IClientHandler
    {
        private Client client;

        public QuestHandler(Client client)
        {
            this.client = client;
        }

        private static MessageTag[] handler =
        [
            MessageTag.CompleteMainQuest, MessageTag.CompleteSideQuest,
            MessageTag.GetDailyQuestRequest, MessageTag.DoneDailyQuestRequest,
            MessageTag.ReloadDailyQuest, MessageTag.WatchAdsSideQuest
        ];

        public MessageTag[] HandleTag => handler;
        private QuestCompleted lastQuestCompleted;
        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            if (tag == MessageTag.CompleteMainQuest)
            {
                var request = reader.Read<int>();
                CompleteMainQuestResult result = null;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        result = charData.CompleteMainQuest(request);
                    }
                }
                catch (InvalidException e)
                {
                    result = new CompleteMainQuestResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.CompleteMainQuestResult, result);
                }
            }
            else if (tag == MessageTag.CompleteSideQuest)
            {
                var request = reader.Read<CompleteSideQuest>();
                CompleteSideQuestResult result = null;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        result = charData.CompleteSideQuest(request);
                        lastQuestCompleted = new QuestCompleted()
                        {
                            Money = result.LinhThachRandom,
                            MoneyVip = result.MoneyVipRandom,
                        };
                    }
                }
                catch (InvalidException e)
                {
                    result = new CompleteSideQuestResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.CompleteSideQuestResult, result);
                }
            }
            else if (tag == MessageTag.GetDailyQuestRequest)
            {
                var eQuest = reader.Read<EDailyQuest>();
                List<DailyQuest> listQuest;
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    listQuest = charData.QuestManager.DailyQuests[eQuest].ToList();
                }
                client.MemSend(MessageTag.GetDailyQuestRequest, listQuest);
            }
            else if (tag == MessageTag.DoneDailyQuestRequest)
            {
                var id = reader.Read<long>();
                DoneQuestResult result = null!;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        charData.QuestManager.ClaimDailyQuest(id);
                        result = new DoneQuestResult()
                        {
                            Success = true,
                            QuestId = id,
                        };
                    }
                }
                catch (InvalidException e)
                {
                    result = new DoneQuestResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.DoneDailyQuestRequest, result);
                }
            }
            else if (tag == MessageTag.ReloadDailyQuest)
            {
                var eDailyQuest = reader.Read<EDailyQuest>();
                bool isNoAds;
                lock (client.CharacterLock)
                {
                    isNoAds = client.CharacterData.IsNoAds;
                }
                if (isNoAds)
                {
                    SendReloadDailyQuest(eDailyQuest);
                }
                else
                {
                    var adsHandler = client.GetHandler<AdsHandler>();
                    ShowAds ads = new ReloadTaskAds("ReloadTask", "Xem quảng cáo để làm mới nhiệm vụ", client, eDailyQuest);
                    adsHandler.SendAds(ads);
                }
            }
            else if (tag == MessageTag.WatchAdsSideQuest)
            {
                try
                {
                    if (lastQuestCompleted == null)
                    {
                        throw new InvalidException(Strings.SideQuestAdRewardNotAvailable);
                    }
                    if (lastQuestCompleted.HasClaimAds)
                    {
                        throw new InvalidException(Strings.SideQuestAdRewardAlreadyClaimed);
                    }

                    if (client.CharacterData.IsNoAds)
                    {
                        ClaimSideQuestAds();
                    }
                    else
                    {
                        var showAds = new WatchAdsSideQuest("AdsSideQuest", "Xem quảng cáo để nhận thưởng x2", client);
                        var adsHandler = client.GetHandler<AdsHandler>();
                        adsHandler.SendAds(showAds);
                    }
                }
                catch (InvalidException e)
                {
                    var result = new WatchAdsSideQuestResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                    client.MemSend(MessageTag.WatchAdsSideQuestResult, result);
                }
            }
        }
        public void ClaimSideQuestAds()
        {
            lastQuestCompleted.HasClaimAds = true;
            var charData = client.CharacterData;
            charData.SetMoneyVip(lastQuestCompleted.MoneyVip + charData.MoneyVip);
            charData.Stats.SetLinhThach(lastQuestCompleted.Money + charData.Stats.LinhThach);
            var result = new WatchAdsSideQuestResult()
            {
                Success = true,
                AddMoney =  lastQuestCompleted.Money,
                AddMoneyVip =  lastQuestCompleted.MoneyVip,
                MoneyVip =  charData.MoneyVip,
                Money = charData.Stats.LinhThach,
            };
            client.MemSend(MessageTag.WatchAdsSideQuestResult, result);
        }
        public void SendReloadDailyQuest(EDailyQuest eDailyQuest)
        {
            ReloadDailyQuestResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    var listQuests = charData.QuestManager.Reload(eDailyQuest);
                    result = new ReloadDailyQuestResult()
                    {
                        Success = true,
                        EDailyQuest =  eDailyQuest,
                        DailyQuests =  listQuests,
                    };
                }
            }
            catch (InvalidException e)
            {
                result = new ReloadDailyQuestResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.ReloadDailyQuestResult, result);
            }
        }
        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
        }

        public void OnEnter()
        {
        }

        public void OnLeave()
        {
        }

    }
}
