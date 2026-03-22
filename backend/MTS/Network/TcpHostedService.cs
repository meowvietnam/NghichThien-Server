using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MTS.Network;

public class TcpHostedService : IHostedService
{
    private readonly TcpServer tcpServer;

    public TcpHostedService()
    {
        tcpServer = new TcpServer(6785);
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Task.Run(() => tcpServer.Run(), cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        tcpServer.Stop();
        return Task.CompletedTask;
    }
}
