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
        Console.WriteLine("App started");
    }
    private void OnStopping()
    {
        Console.WriteLine("App stopping...");
    }

    private void OnStopped()
    {
        Console.WriteLine("App stopped");
    }
}
