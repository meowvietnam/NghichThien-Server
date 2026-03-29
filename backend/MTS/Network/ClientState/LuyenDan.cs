using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class LuyenDan : IClientState
{
    private Client client;

    public LuyenDan(Client client)
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
                    Success = false,
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

    public MessageState State => MessageState.LuyenDan;
}