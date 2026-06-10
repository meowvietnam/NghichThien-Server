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
               MessageTag.CompleteMainQuest,
        ];

        public MessageTag[] HandleTag => handler;

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            var charData = client.CharacterData;
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
