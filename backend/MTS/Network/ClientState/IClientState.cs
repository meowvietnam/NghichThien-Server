using Network;

namespace MTS.Network.ClientState
{
    public interface IClientState : IMessageState<MessageTag,MessageState>
    {
        void OnDisconnect(IProtocol<MessageTag> protocol);
    }
}