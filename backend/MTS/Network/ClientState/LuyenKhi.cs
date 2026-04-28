using MTS.Network.Models;
using Network;
using Network.Extensions;
namespace MTS.Network.ClientState;
public class LuyenKhi : IClientState
{
    private Client client;
    private bool isLuyenKhi;
    public LuyenKhi(Client client)
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
        if (tag == MessageTag.LuyenKhi)
        {
            if(isLuyenKhi) return;
            try
            {
                isLuyenKhi = true;
                var request = reader.Read<LuyenKhiRequest>();
                var inventory = await client.GetInventory();
                var result = inventory.LuyenKhi(request);
                client.Send(MessageTag.LuyenKhiResult, result);
            }
            catch (Exception e)
            {
                client.Send(MessageTag.LuyenKhiResult, new LuyenKhiResult
                {
                    Success = false,
                    Message = e.Message,
                });
            }
            finally
            {
                isLuyenKhi = false;
            }
        }
        if (tag == MessageTag.SetEquipmentName)
        {
            var request = reader.Read<SetEquipmentName>();
            var inventory = await client.GetInventory();
            var result = inventory.SetEquipmentName(request);
            client.Send(MessageTag.SetEquipmentNameResult, result);
        }
    }
    public void OnLeave()
    {
    }
    public MessageState State => MessageState.LuyenKhi;
}