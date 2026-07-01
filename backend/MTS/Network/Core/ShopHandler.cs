using MTS.Models;

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
            try
            {
                lock (shopManager)
                {
                    var shop = shopManager.ReloadShop(shopType);
                    client.MemSend(MessageTag.ReloadShopRequest, shop);
                }
            }
            catch (Exception ex)
            {
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
        MessageTag.ReloadShopRequest
    ];

    public MessageTag[] HandleTag => handler;
}