using MTS.Exceptions;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class ProgressionHandler : IClientHandler
{
    private readonly Client client;

    public ProgressionHandler(Client client)
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
        if (tag == MessageTag.SpecialLevelUpRequest)
        {
            SpecialLevelUpResult result = null;
            try
            {
                lock (client.CharacterLock)
                {
                    var charData = client.CharacterData;
                    result = charData.Progression.SpecialLevelUp();
                }
            }
            catch (InvalidException e)
            {
                result = new SpecialLevelUpResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.SpecialLevelUpRequest, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    private static readonly MessageTag[] handler = { MessageTag.SpecialLevelUpRequest };
    public MessageTag[] HandleTag => handler;

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }
}
