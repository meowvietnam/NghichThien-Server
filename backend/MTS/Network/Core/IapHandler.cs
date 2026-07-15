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
              MessageTag.GetProductsRequest,
              MessageTag.ExchangeMoneyVipRequest
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
            if (tag == MessageTag.IapPurchase)
            {
                var request = reader.Read<IapModel>();
                IapResult result = null;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
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
                    Console.WriteLine(result);
                    client.Send(MessageTag.PurchaseIapResult, result);
                }
            }
            else if (tag == MessageTag.GetProductsRequest)
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    var ownerProducts = charData.GetProducts().GetAwaiter().GetResult();
                    client.MemSend(MessageTag.GetProductsRequest, ownerProducts);
                }
            }
            else if (tag == MessageTag.ExchangeMoneyVipRequest)
            {
                ExchangeMoneyVipResult result = null!;
                try
                {
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        var amount = reader.Read<int>();
                        result = charData.ExchangeMoneyVipForLinhThach(amount);
                    }
                }
                catch (Exception e)
                {
                    result = new ExchangeMoneyVipResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    client.MemSend(MessageTag.ExchangeMoneyVipRequest, result);
                }
            }
        }

        public void OnLeave()
        {
        }
    }
}
