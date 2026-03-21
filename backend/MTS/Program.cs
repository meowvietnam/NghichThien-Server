using NetworkServer;

namespace MTS;

public static class Program
{
    public static void Main(string[] args)
    {
        var app = new Application();
        
        var tcpServer = new TcpServer(6785);
        app.AddServer(tcpServer);

        var httpServer = new HttpServer(9481);
        app.AddServer(httpServer);

        app.RunAll();

        while (true)
        {
            Console.ReadLine();
        }
    }
}