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
        else if (tag == MessageTag.SetNgoDaoDiem)
        {
            var ngoDaoDiem = reader.Read<int>();
            client.CharacterData.Progression.SetNgoDaoDiem(ngoDaoDiem);
            client.Send(MessageTag.SetNgoDaoDiemResult, ngoDaoDiem);
        }
        else if (tag == MessageTag.SetCamNgo)
        {
            var camNgo = reader.Read<CamNgo>();
            var inventory = await client.GetInventory();
            inventory.AddOrUpdate(camNgo);
            client.Send(MessageTag.SetCamNgoResult, camNgo);
        }
        else if (tag == MessageTag.AddThanThong)
        {
            var id = reader.Read<ushort>();
            var inventory = await client.GetInventory();
            if (inventory.EquipThanThong(id))
            {
                client.Send(MessageTag.AddThanThongResult, id);
            }
        }
        else if (tag == MessageTag.AddCongPhap)
        {
            var congPhap = reader.Read<CongPhap>();
            if (congPhap.Level == 0) return;
            client.CharacterData.InventoryManager.AddOrUpdate(congPhap);
            client.Send(MessageTag.AddCongPhapResult, congPhap);
        }
    }

    public void OnLeave()
    {
    }

    private static readonly MessageTag[] handler =
    [
        MessageTag.AddItems, MessageTag.SetInfinityEnergy, MessageTag.SetNgoDaoDiem, MessageTag.SetCamNgo,
        MessageTag.AddThanThong, MessageTag.AddCongPhap
    ];

    public MessageTag[] HandleTag => handler;

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}