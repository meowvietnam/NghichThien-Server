using MTS.Models;
using MTS.Network.ClientState;
using Network;

namespace MTS.Network;

public class Client : ClientConnection, ISender<MessageTag>
{
    public int Id { get; private set; }
    public string Key { get; private set; }
    private MessageStateHandler<MessageTag,MessageState> stateModule;
    public UserData UserData { get; private set; }
    public Client(UnauthenticatedClient client)
    {
        Id = client.Id;
        Key = client.Key;
        var tcpClient = client.TcpClient;
        client.TransferProtocol(tcpClient,this);
        TcpClient = tcpClient;
        var coreModule = new CoreMessageHandler<MessageTag>();
        stateModule = new MessageStateHandler<MessageTag,MessageState>();
        AddModule(coreModule);
        AddModule(stateModule);
    }
    public void ChangeState(IClientState state)
    {
        stateModule.ChangeState(state);
    }

    public void SetUserData(UserData userData)
    {
        UserData = userData;
    }
    protected override void OnError(IProtocol<MessageTag> protocol, Exception ex)
    {
        Console.WriteLine(ex);
    }
    protected override void OnDisconnected(IProtocol<MessageTag> protocol)
    {
        Console.WriteLine($"Client {Id} has been disconnected.");
        TcpServer.Instance.Remove(this);
    }

    public void Send(Span<byte> data)
    {
        TcpClient.Send(data);
    }
}