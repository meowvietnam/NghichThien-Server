using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class LuyenDan : IClientState
{
    private Client client;
    private bool isLuyenDan;
    private readonly object luyenDanLock = new();
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
            lock (luyenDanLock)
            {
                if (isLuyenDan) return;
                isLuyenDan = true;
            }
            try
            {
                var request = reader.Read<LuyenDanRequest>();
                LuyenDanResult result;
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.InventoryManager.Refine(charData, request);
                    if (result.Success)
                    {
                        charData.Stats.SetTuoiTac(result.NewTuoiTac);
                    }
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
                lock (luyenDanLock)
                {
                    isLuyenDan = false;
                }
            }
        }
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.LuyenDan;
}
