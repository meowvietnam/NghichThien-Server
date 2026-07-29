using MTS.Exceptions;
using MTS.Game.Rank;
using MTS.Localization;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class SyncHandler : IClientHandler
{
    private readonly Client client;

    public SyncHandler(Client client)
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
        if (tag == MessageTag.SyncCharacterData)
        {
            SyncCharacterDataResult result = null!;
            try
            {
                lock (client.CharacterLock)
                {
                    if (!client.IsCharacterSelected())
                    {
                        throw new InvalidException(Strings.CharacterNotSelected);
                    }

                    var wrapper = new CharacterWrapper(client.CharacterData);
                    result = new SyncCharacterDataResult()
                    {
                        Success = true,
                        Character = wrapper,
                    };
                }
            }
            catch (InvalidException e)
            {
                result = new SyncCharacterDataResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.SyncCharacterDataResult, result);
            }
        }
    }

    public void OnLeave()
    {
    }

    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
    }

    private static MessageTag[] handler = [MessageTag.SyncCharacterData];
    public MessageTag[] HandleTag => handler;
}
