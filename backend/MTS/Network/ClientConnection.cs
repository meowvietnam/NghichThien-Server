using Network;
using NativeTcp = System.Net.Sockets.TcpClient;
namespace MTS.Network;

public abstract class ClientConnection : BaseClient<MessageTag> 
{
    public TcpClient TcpClient { get; protected set; }

    public ClientConnection()
    {
        
    }
    public ClientConnection(NativeTcp client)
    {
        TcpClient = new TcpClient(client);
        AddProtocol(TcpClient);
        TcpClient.StartRead();
    }
}