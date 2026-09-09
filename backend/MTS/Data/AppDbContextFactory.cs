using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MTS.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Generate migrations without connecting to an application database.
        if (args.Contains("--offline-migrations"))
        {
            var offline = new DbContextOptionsBuilder<AppDbContext>();
            offline.UseMySql("Server=127.0.0.1;Database=mts_schema_only;User=migration;Password=unused",
                new MySqlServerVersion(new Version(8, 0, 36)));
            return new AppDbContext(offline.Options);
        }
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.{env}.json")
            .Build();
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new AppDbContext(builder.Options);
    }
}
