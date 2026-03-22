using MTS.Game;
using NetworkServer;

namespace MTS.Network;

public class TcpServer : BaseTcpServer
{
    public static TcpServer Instance { get; private set; }
    public TcpServer(int port) : base(port)
    {
        Instance = this;
    }
    protected override void OnStop()
    {
        Console.WriteLine($"TcpServer is stopped");
    }
    protected override void OnRun()
    {
        Console.WriteLine($"TcpServer is running at port: {Port}");
    }
}