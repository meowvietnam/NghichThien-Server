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
        if (tag == MessageTag.LoadShopRequest)
        {
            var shopType = reader.Read<EShopType>();
            Shop shop;
            lock (client.CharacterLock)
            {
                var charData = client.CharacterData;
                shop = charData.ShopManager.GetShop(shopType);
            }
            client.MemSend(MessageTag.LoadShopRequest, shop);
        }
        else if (tag == MessageTag.ReloadShop)
        {
            var shopType = reader.Read<EShopType>();
            bool isNoAds;
            lock (client.CharacterLock)
            {
                isNoAds = client.CharacterData.IsNoAds;
            }
            if (isNoAds)
            {
                SendReloadShop(shopType);
            }
            else
            {
                ShowAds ads = new ReloadShopAds("ReloadShop", "Xem quảng cáo để làm mới cửa hàng", client, shopType);
                var adsHandler = client.GetHandler<AdsHandler>();
                adsHandler.SendAds(ads);
            }
        }
        else if (tag == MessageTag.BuyAndSellRequest)
        {
            var request = reader.Read<BuyAndSell>();
            BuyAndSellResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
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

    public void SendReloadShop(EShopType shopType)
    {
        ReloadShopResult result = null!;
        try
        {
            lock (client.CharacterLock)
            {
                var charData = client.CharacterData;
                var shopManager = charData.ShopManager;
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
            client.MemSend(MessageTag.ReloadShopResult, result);
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
        MessageTag.ReloadShop,
        MessageTag.BuyAndSellRequest
    ];

    public MessageTag[] HandleTag => handler;
}
