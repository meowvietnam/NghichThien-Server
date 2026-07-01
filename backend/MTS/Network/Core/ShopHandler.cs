using MTS.Models;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class ShopHandler : IClientHandler
{
    private Client client;

    public ShopHandler(Client client)
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
        var charData = client.CharacterData;
        if (tag == MessageTag.LoadShopRequest)
        {
            var shopType = reader.Read<EShopType>();
            var shop = charData.ShopManager.GetShop(shopType);
            client.MemSend(MessageTag.LoadShopRequest, shop);
        }
        else if (tag == MessageTag.ReloadShopRequest)
        {
            var shopManager = charData.ShopManager;
            var shopType = reader.Read<EShopType>();
            ReloadShopResult result = null!;
            try
            {
                lock (shopManager)
                {
                    var shop = shopManager.ReloadShop(shopType);
                    result = new ReloadShopResult()
                    {
                        Success = true,
                        Shop = shop,
                    };
                }
            }
            catch (Exception ex)
            {
                result = new ReloadShopResult()
                {
                    Success = false,
                    Message = ex.Message,
                };
            }
            finally
            {
                client.MemSend(MessageTag.ReloadShopRequest, result);
            }
        }
        else if (tag == MessageTag.BuyAndSellRequest)
        {
            var request = reader.Read<BuyAndSell>();
            BuyAndSellResult result = null;
            try
            {
                lock (charData)
                {
                    result = charData.ShopManager.BuyAndSell(request);
                }
            }
            catch (Exception ex)
            {
                result = new BuyAndSellResult()
                {
                    Success = false,
                    Message = ex.Message,
                };
            }
            finally
            {
                client.MemSend(MessageTag.BuyAndSellRequest, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }

    private static readonly MessageTag[] handler =
    [
        MessageTag.LoadShopRequest,
        MessageTag.ReloadShopRequest,
        MessageTag.BuyAndSellRequest
    ];

    public MessageTag[] HandleTag => handler;
}