using MTS.Exceptions;
using MTS.Localization;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class TrungSinhHandler : IClientHandler
{
    private Client client;

    public TrungSinhHandler(Client client)
    {
        this.client = client;
    }
    public void OnEnter()
    {
        
    }

    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        if (tag == MessageTag.GetTrungSinhConfigRequest)
        {
            GetTrungSinhConfig result = null;
            try
            {
                var config = client.CharacterData.GetTrungSinhConfig();
                result = new GetTrungSinhConfig()
                {
                    Success = true,
                    Config = config
                };
            }
            catch (InvalidException e)
            {
                result = new GetTrungSinhConfig()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.GetTrungSinhConfigRequest, result);
            }
        }
        else if (tag == MessageTag.ToTrungSinhRequest)
        {
            Result result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    var config = charData.GetTrungSinhConfig();
                    if (config.MoneyVip > charData.MoneyVip)
                    {
                        throw new InvalidException(Strings.NotEnoughMoneyVip);
                    }
                    var congPhaps = charData.EquipmentManager.GetAllSlotsCP();
                    if (congPhaps.Any(x => x != ushort.MaxValue))
                    {
                        throw new InvalidException("Hãy tháo hết công pháp");
                    }
                    client.ChangeState(new TrungSinh(client));
                    result = new Result()
                    {
                        Success = true,
                    };
                }
            }
            catch (InvalidException e)
            {
                result = new Result()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.ToTrungSinhRequest, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    private static readonly MessageTag[] handlers = [
        MessageTag.GetTrungSinhConfigRequest,
        MessageTag.ToTrungSinhRequest
    ];
    public MessageTag[] HandleTag => handlers;
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}