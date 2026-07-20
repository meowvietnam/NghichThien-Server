using MTS.Exceptions;
using MTS.Localization;
using MTS.Network.ClientState;
using MTS.Network.Models;

namespace MTS.Network.Core;

public class InGameAuthHandler : IClientHandler
{
    private Client client;
    public InGameAuthHandler(Client client)
    {
        this.client = client;
    }
    public void OnEnter()
    {
    }

    private async Task<LogoutResult> ClientLogout()
    {
        if (!client.IsCharacterSelected())
        {
            throw new InvalidException(Strings.CharacterNotSelected);
        }
        await client.SaveChanges();
        client.SetUserData(null);
        client.SetUserData(null);
        client.ChangeState(new AuthState(client));
        return new LogoutResult()
        {
            Success = true
        };
    }
    public void Handle(Message<MessageTag> message)
    {
        var tag = message.Tag;
        var reader = message.Reader;
        if (tag == MessageTag.InGameLogout)
        {
            LogoutResult result = null;
            try
            {
                lock (client.StateLock)
                {
                    result = ClientLogout().GetAwaiter().GetResult();
                }
            }
            catch (InvalidException e)
            {
                result = new LogoutResult()
                {
                    Success = false,
                    Message = e.Message
                };
            }
            finally
            {
                client.MemSend(MessageTag.InGameLogoutResult, result);
            }
        }
    }

    public void OnLeave()
    {
    }
    private static MessageTag[] handlers = { MessageTag.InGameLogout };
    public MessageTag[] HandleTag => handlers;
    public void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        
    }
}
