using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class LuyenDan : IClientState
{
    private Client client;
    private bool isLuyenDan;
    public LuyenDan(Client client)
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
        if (tag == MessageTag.LuyenDan)
        {
            if(isLuyenDan) return;
            try
            {
                var request = reader.Read<LuyenDanRequest>();
                isLuyenDan = true;
                var charData = client.CharacterData;
                var result = charData.InventoryManager.Refine(charData, request);
                if (result.Success)
                {
                    charData.Stats.TuoiTac = result.NewTuoiTac;
                }

                client.Send(MessageTag.LuyenDanResult, result);
            }
            catch (Exception e)
            {
                client.Send(MessageTag.LuyenDanResult, new LuyenDanResult()
                {
                    Success = false,
                    Message = e.Message,
                });
            }
            finally
            {
                isLuyenDan = false;
            }
        }
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.LuyenDan;
}