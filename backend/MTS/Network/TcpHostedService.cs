namespace MTS.Network;

public class TcpHostedService : IHostedService
{
    private readonly TcpServer tcpServer;
    public TcpHostedService(IConfiguration config)
    {
        var port = config.GetValue("ServerPorts:TcpPort", 6785);
        tcpServer = new TcpServer(port);
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => tcpServer.Run(), cancellationToken);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        tcpServer.Stop();
        await tcpServer.SaveClients();
    }
}
