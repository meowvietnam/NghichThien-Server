using MTS.Models;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core
{
    public class MonPhaiHandler : IClientHandler
    {
        private Client client;

        private static MessageTag[] handler =
        [
                MessageTag.JoinMonPhai
        ];
        public MonPhaiHandler(Client client)
        {
            this.client = client;
        }
        public MessageTag[] HandleTag => handler;
        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            var charData = client.CharacterData;
            if(tag == MessageTag.JoinMonPhai)
            {
                var request = reader.Read<EMonPhai>();
                JoinMonPhaiResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.JoinMonPhai(request);
                    }
                }
                catch (Exception e)
                {
                    result = new JoinMonPhaiResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.JoinMonPhaiResult, result);
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
