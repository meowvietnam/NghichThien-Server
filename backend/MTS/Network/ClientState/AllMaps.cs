using Network;

namespace MTS.Network.ClientState;

public class AllMaps : IClientState
{
    private Client client;
    public AllMaps(Client client)
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
        
    }

    public void OnLeave()
    {
    }

    public MessageState State => MessageState.AllMaps;
}