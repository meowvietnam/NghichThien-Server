using Network;

namespace MTS.Network.ClientState;
public class LuyenKhi : IClientState
{
    private Client client;
    public LuyenKhi(Client client)
    {
        this.client = client;
    }
    public void OnEnter()
    {
    }
    public async void Handle(Message<MessageTag> message)
    {

    }
    public void OnLeave()
    {
    }
    public MessageState State => MessageState.LuyenDan;
}