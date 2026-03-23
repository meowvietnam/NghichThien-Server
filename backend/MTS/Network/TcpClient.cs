using Network;
using NativeTcp = System.Net.Sockets.TcpClient;
namespace MTS.Network;
public class TcpClient : Tcp<MessageTag>
{
    public TcpClient(NativeTcp client) : base(client)
    {
    }
}