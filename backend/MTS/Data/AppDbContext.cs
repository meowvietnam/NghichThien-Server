using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Models;
using Newtonsoft.Json;

namespace MTS.Data;

public class AppDbContext : DbContext
{
    public DbSet<DeveloperAccount> DeveloperAccounts { get; set; }
    public DbSet<UserData> Users { get; private set; }
    public DbSet<CharacterData> Characters { get; private set; }
    public DbSet<CharacterStats> CharacterStats { get; private set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    private static string SerializeObject<T>(T value)
    {
        return JsonConvert.SerializeObject(value);
    }

    private static T DeserializeObject<T>(string json) where T : new()
    {
        if (string.IsNullOrWhiteSpace(json))
            return new T();

        try
        {
            return JsonConvert.DeserializeObject<T>(json) ?? new T();
        }
        catch
        {
            return new T();
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeveloperAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasOne(x => x.UserData)
                .WithOne()
                .HasForeignKey<DeveloperAccount>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
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
                    v => SerializeObject(v),
                    v => DeserializeObject<Dictionary<string, string>>(v)
                );

            e.Property(x => x.Colors)
                .HasColumnType("json")
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<Dictionary<string, Color>>(v)
                );
            e.Property(x => x.NgoDaoSaves)
                .HasColumnType("json")
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<List<int>>(v)
                );
            e.Property(x => x.CamNgoSaves)
                .HasColumnType("json")
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<Dictionary<int, int>>(v)
                );
            e.Property(x => x.ThanThongOwner)
                .HasColumnType("json")
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<List<int>>(v)
                );
            e.Property(x => x.ThanThongEquip)
                .HasColumnType("json")
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<List<int>>(v)
                );
            e.Property(x => x.CongPhapOwner)
                .HasColumnType("json")
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<Dictionary<int, int>>(v)
                );
            e.Property(x => x.DanCombatUsed)
                .HasColumnType("json")
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<List<int>>(v)
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