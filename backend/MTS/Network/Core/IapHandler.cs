using MTS.Exceptions;
using MTS.IAP;
using MTS.Models;
using MTS.Network.Models;
using Network;
using Network.Extensions;
using Serilog;

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
                    var verifiedPurchase = CharacterData.VerifyIapPurchase(request).GetAwaiter().GetResult();
                    lock (client.CharacterLock)
                    {
                        var charData = client.CharacterData;
                        result = charData.CompleteIapPurchase(verifiedPurchase).GetAwaiter().GetResult();
                    }
                }
                catch (InvalidException e)
                {
                    result = new IapResult()
                    {
                        Success = false,
                        Message = e.Message
                    };
                }
                finally
                {
                    Log.Information(
                        "IAP purchase handled; ClientId={ClientId}, CharacterId={CharacterId}, Success={Success}, Store={Store}, ProductId={ProductId}",
                        client.Id, client.CharacterData?.Id, result?.Success, request.Store, request.ProductId);
                    client.Send(MessageTag.PurchaseIapResult, result);
                }
            }
            else if (tag == MessageTag.GetProductsRequest)
            {
                Store store;
                try
                {
                    store = reader.Read<Store>();
                }
                catch (Exception e)
                {
                    store = Store.GooglePlay;
                }
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    var ownerProducts = charData.GetProducts(store).GetAwaiter().GetResult();
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
                catch (InvalidException e)
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