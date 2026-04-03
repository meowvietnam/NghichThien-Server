using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core;

public class InventoryHandler : IClientHandler
{
    private Client client;

    public InventoryHandler(Client client)
    {
        this.client = client;
    }
    public void OnEnter()
    {
    }
    public async void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.GetInventory)
        {
            try
            {
                var inventory = await client.GetInventory();
                var result = new GetInventoryResult()
                {
                    Success = true,
                    InventoryManager = inventory,
                };
                client.Send(MessageTag.GetInventoryResult, result);
            }
            catch (Exception e)
            {
                var result = new GetInventoryResult()
                {
                    Success = false,
                    Message = e.Message
                };
                client.Send(MessageTag.GetInventoryResult, result);
            }
        }
    }
    public void OnLeave()
    {
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
    private static readonly MessageTag[] handlerTag = { MessageTag.GetInventory };
    public MessageTag[] HandleTag => handlerTag;
}