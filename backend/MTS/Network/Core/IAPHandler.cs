using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core
{
    public class IAPHandler : IClientHandler
    {
        private Client client;
        private static MessageTag[] handler =
        [
              MessageTag.IAP
        ];

        public MessageTag[] HandleTag => throw new NotImplementedException();

        public IAPHandler(Client client)
        {
            this.client = client;
        }

        public void OnDisconnect(IProtocol<MessageTag> protocol)
        {
        }

        public void OnEnter()
        {
        }

        public void Handle(Message<MessageTag> message)
        {
            var tag = message.Tag;
            var reader = message.Reader;
            var charData = client.CharacterData;
            if (tag == MessageTag.IAP)
            {
                var request = reader.Read<IAPModel>();
                IAPResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.IAP(request);
                    }
                }
                catch (Exception e)
                {
                    result = new IAPResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.IAPResult, result);
                }
            }
        }

        public void OnLeave()
        {
        }
    }
}
