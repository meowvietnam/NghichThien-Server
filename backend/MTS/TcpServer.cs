using NetworkServer;

namespace MTS;

public class TcpServer : BaseTcpServer
{
    public TcpServer(int port) : base(port)
    {
    }

    protected override void OnRun()
    {
        Console.WriteLine($"Server is running at port: {Port}");
    }
}