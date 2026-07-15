using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.IAP;
using MTS.Models;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Network;

public class TcpHostedService : IHostedService
{
    private readonly TcpServer tcpServer;

    public TcpHostedService(IConfiguration config)
    {
        var port = config.GetValue("ServerPorts:TcpPort", 6785);
        tcpServer = new TcpServer(port);
    }

    private async Task StartTask(CancellationToken stoppingToken)
    {
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await StartTask(cancellationToken);
        await Task.Run(() => tcpServer.Run(), cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        tcpServer.Stop();
        await tcpServer.SaveClients();
    }
}