using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Core;

public class AdminHandler : IClientHandler
{
    private Client client;

    public AdminHandler(Client client)
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
        if (tag == MessageTag.AddItems)
        {
            var items = reader.ReadArray<ItemRef>().ToList();
            if (!items.Any())
            {
                return;
            }

            var inventory = await client.GetInventory();
            var rewards = inventory.AddToInventory(items);
            client.Send(MessageTag.AddItemsResult, rewards);
        }
        else if (tag == MessageTag.SetInfinityEnergy)
        {
            var infinityEnergy = reader.Read<bool>();
            client.UserData.SetInfiniteEnergy(infinityEnergy);
        }
    }

    public void OnLeave()
    {
    }

    private static readonly MessageTag[] handler = [MessageTag.AddItems, MessageTag.SetInfinityEnergy];
    public MessageTag[] HandleTag => handler;

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}