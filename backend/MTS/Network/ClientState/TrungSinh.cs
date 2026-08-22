using MTS.Exceptions;
using MTS.Game.CreateCharacter;
using MTS.Game.Rank;
using MTS.Network.Models;
using MTS.Services;

namespace MTS.Network.ClientState;

public class TrungSinh : IClientState
{
    private Client client;
    public TrungSinh(Client client)
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
        if (tag == MessageTag.TrungSinhRequest)
        {
            var models = reader.MemRead<List<SelectionModel>>();
            TrungSinhResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    client.CharacterData.TrungSinh(models).GetAwaiter().GetResult();
                    result = new TrungSinhResult()
                    {
                        Success = true,
                        Wrapper = new CharacterWrapper(client.CharacterData),
                    };
                }
            }
            catch (InvalidException e)
            {
                result = new TrungSinhResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.TrungSinhRequest, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.TrungSinh;
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}