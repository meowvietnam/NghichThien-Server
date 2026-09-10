using Network;
using NativeTcp = System.Net.Sockets.TcpClient;
namespace MTS.Network;
public class TcpClient : Tcp<MessageTag>
{
    public event Action ReconnectedSuccess;
    public TcpClient(NativeTcp client) : base(client)
    {
    }
    protected override void OnReconnected()
    {
        ReconnectedSuccess?.Invoke();
    }
}