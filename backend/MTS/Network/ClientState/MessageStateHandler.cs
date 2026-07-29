namespace MTS.Network.ClientState;

public class MessageStateHandler : MessageStateHandler<MessageTag, MessageState>
{
    public override void OnDisconnect(IProtocol<MessageTag> protocol)
    {
        if (State is IClientState state)
        {
            state.OnDisconnect(protocol);
        }
    }
}