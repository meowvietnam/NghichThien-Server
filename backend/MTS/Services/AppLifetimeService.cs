namespace MTS.Services;

public class AppLifetimeService
{
    public AppLifetimeService(IHostApplicationLifetime lifetime)
    {
        lifetime.ApplicationStarted.Register(OnStarted);
        lifetime.ApplicationStopping.Register(OnStopping);
        lifetime.ApplicationStopped.Register(OnStopped);
    }
    private void OnStarted()
    {
        Serilog.Log.Information("App started");
    }
    private void OnStopping()
    {
        Serilog.Log.Information("App stopping");
    }

    private void OnStopped()
    {
        Serilog.Log.Information("App stopped");
    }
}
