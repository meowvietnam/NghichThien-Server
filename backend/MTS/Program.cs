using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MTS.Data;
using NetworkServer;

namespace MTS;

public static class Program
{
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
            sqlOptions =>
            {
                sqlOptions.CommandTimeout(30);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });

        var permanentDb = new AppDbContext(optionsBuilder.Options);

        var app = new Application();
        
        var tcpServer = new TcpServer(6785, permanentDb);
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