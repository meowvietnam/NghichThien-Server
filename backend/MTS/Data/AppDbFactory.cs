using Microsoft.EntityFrameworkCore;

namespace MTS.Data;

public static class AppDbFactory
{
    private static DbContextOptions<AppDbContext>? _options;
    public static void Initialize(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }
    public static AppDbContext Create()
    {
        if (_options == null)
            throw new InvalidOperationException("AppDbFactory chưa được Initialize trong Program.cs!");
            
        return new AppDbContext(_options);
    }
}
