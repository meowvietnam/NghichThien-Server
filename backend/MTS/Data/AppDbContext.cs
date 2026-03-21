using Microsoft.EntityFrameworkCore;
using MTS.Models;

namespace MTS.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; private set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
                
    }
}
