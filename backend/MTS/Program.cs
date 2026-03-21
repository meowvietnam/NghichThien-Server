using NetworkServer;

namespace MTS;

public static class Program
{
    public static void Main(string[] args)
    {
        var app = new Application();
        
        var tcpServer = new TcpServer(9000);
        app.AddServer(tcpServer);

        var httpServer = new HttpServer(443);
        app.AddServer(httpServer);

        app.RunAll();

        while (true)
        {
            Console.ReadLine();
        }
    }
}