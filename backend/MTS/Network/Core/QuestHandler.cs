using MTS.Models;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core
{
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
            MessageTag.ReloadDailyQuestRequest,
        ];

        public MessageTag[] HandleTag => handler;

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            var charData = client.CharacterData;
            var questManager = charData.QuestManager;
            if (tag == MessageTag.CompleteMainQuest)
            {
                var request = reader.Read<int>();
                CompleteMainQuestResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.CompleteMainQuest(request);
                    }
                }
                catch (Exception e)
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
                    lock (charData)
                    {
                        result = charData.CompleteSideQuest(request);
                    }
                }
                catch (Exception e)
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
                var listQuest = questManager.DailyQuests[eQuest];
                client.MemSend(MessageTag.GetDailyQuestRequest, listQuest);
            }
            else if (tag == MessageTag.DoneDailyQuestRequest)
            {
                var id = reader.Read<long>();
                DoneQuestResult result = null!;
                try
                {
                    lock (charData)
                    {
                        charData.QuestManager.ClaimDailyQuest(id);
                        result = new DoneQuestResult()
                        {
                            Success = true,
                            QuestId = id,
                        };
                    }
                }
                catch (Exception e)
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
            else if (tag == MessageTag.ReloadDailyQuestRequest)
            {
                var eDailyQuest = reader.Read<EDailyQuest>();
                lock (charData)
                {
                    var listQuests = charData.QuestManager.Reload(eDailyQuest);
                    client.MemSend(MessageTag.ReloadDailyQuestRequest, listQuests);
                }
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