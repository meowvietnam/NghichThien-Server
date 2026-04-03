using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.ClientState;

public class LuyenDan : IClientState
{
    private Client client;

    public LuyenDan(Client client)
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