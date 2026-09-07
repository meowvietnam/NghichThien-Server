using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Network.ClientState;

public class ConvertDanLuState : IClientState
{
    private readonly Client client;

    public ConvertDanLuState(Client client)
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
        if (tag == MessageTag.ConvertDanLuRequest)
        {
            var request = reader.MemRead<ConvertDanLuRequest>();
            ConvertDanLuResult result = null!;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.ConvertDanLu(request);
                }
            }
            catch (InvalidException e)
            {
                result = new ConvertDanLuResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.ConvertDanLuRequest, result);
            }
        }
    }
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        
    }
    public void OnLeave()
    {
    }
    public MessageState State => MessageState.ConvertDanLu;
}
