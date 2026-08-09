using MTS.Models;

namespace MTS.Services;

public class Startup : BackgroundService
{
    private readonly ILogger<Startup> logger;

    public Startup(ILogger<Startup> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var db = await Program.CreateDbAsync();
    }
}