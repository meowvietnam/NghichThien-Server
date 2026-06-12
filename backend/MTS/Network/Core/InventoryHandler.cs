using MTS.Network.Models;
using Network;
using Network.Extensions;
using static JetBrains.FormatRipper.FileExplorer.FileTypeExplorer;

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
        var charData = client.CharacterData;
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
        else if (tag == MessageTag.EquipEquipment)
        {
            var request = reader.Read<EquipEquipment>();
            EquipEquipmentResult result = null;
            try
            {
                lock (charData)
                {

                    result = charData.EquipEquipment(request);

                }
            }
            catch (Exception e)
            {
                result = new EquipEquipmentResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.Send(MessageTag.EquipEquipmentResult, result);
            }
        }    
    }
    public void OnLeave()
    {
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
    private static readonly MessageTag[] handlerTag = { MessageTag.GetInventory, MessageTag.EquipEquipment };
    public MessageTag[] HandleTag => handlerTag;
}