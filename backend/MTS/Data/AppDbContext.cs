using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Models;
using Newtonsoft.Json;

namespace MTS.Data;

public class AppDbContext : DbContext
{
    public DbSet<UserData> Users { get; private set; }
    public DbSet<CharacterData> Characters { get; private set; }
    public DbSet<CharacterStats> CharacterStats { get; private set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserData>((e) =>
        {
            e.HasKey(x => x.Id);
        });
        modelBuilder.Entity<CharacterData>((e) =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Skins)
                .HasColumnType("json")
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<string, string>>(v)
                );

            e.Property(x => x.Colors)
                .HasColumnType("json")
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<string, Color>>(v)
                );
            
            e.HasOne(x => x.User)
                .WithMany(x => x.CharacterDatas)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);;
        });
        modelBuilder.Entity<CharacterStats>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharacterData)
                .WithOne(x => x.Stats)
                .HasForeignKey<CharacterStats>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);;
        });
    }
}