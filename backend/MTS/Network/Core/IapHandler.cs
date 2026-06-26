using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core
{
    public class IapHandler : IClientHandler
    {
        private Client client;
        private static MessageTag[] handler =
        [
              MessageTag.IapPurchase,
              MessageTag.GetProductsRequest
        ];

        public MessageTag[] HandleTag => handler;

        public IapHandler(Client client)
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
            if (tag == MessageTag.IapPurchase)
            {
                var request = reader.Read<IapModel>();
                IapResult result = null;
                try
                {
                    lock (charData)
                    {
                        result = charData.IapPurchase(request).GetAwaiter().GetResult();
                    }
                }
                catch (Exception e)
                {
                    result = new IapResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.Send(MessageTag.PurchaseIapResult, result);
                }
            }
            else if (tag == MessageTag.GetProductsRequest)
            {
                lock (charData)
                {
                    var ownerProducts = charData.GetProducts().GetAwaiter().GetResult();
                    client.MemSend(MessageTag.GetProductsRequest, ownerProducts);
                }
            }
        }

        public void OnLeave()
        {
        }
    }
}
