using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MTS.Game.LuyenKhi;
using MTS.Models;
using MTS.Utils;
using Newtonsoft.Json;

namespace MTS.Data;

public class AppDbContext : DbContext
{
    public DbSet<DeveloperAccount> DeveloperAccounts { get; set; }
    public DbSet<UserData> Users { get; private set; }
    public DbSet<CharacterData> Characters { get; private set; }
    public DbSet<Progression> Progressions { get; private set; }
    public DbSet<Appearance> Appearances { get; private set; }
    public DbSet<CharacterStats> CharacterStats { get; private set; }
    public DbSet<InventoryManager> InventoryManagers { get; private set; }
    public DbSet<ItemRef> Items { get; private set; }
    public DbSet<FurnaceData> Furnaces { get; private set; }
    public DbSet<Mail> Mails { get; private set; }
    public DbSet<Social> Socials { get; private set; }
    public DbSet<Relationship> Relationships { get; private set; }
    public DbSet<CamNgo> CamNgos { get; private set; }
    public DbSet<CongPhap> CongPhaps { get; private set; }
    public DbSet<NgoDao> NgoDaos { get; private set; }
    public DbSet<ThanThong> ThanThongs { get; private set; }
    public DbSet<EquipmentManager> EquipmentManagers { get; private set; }
    public DbSet<ThanThongEquip> ThanThongEquips { get; private set; }
    public DbSet<DanCombatUsed> DanCombatUseds { get; private set; }
    public DbSet<Equipment> Equipments { get; private set; }
    public DbSet<QuestManager> QuestManagers { get; private set; }

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

    private static T[] DeserializeArray<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<T>();
        try
        {
            return JsonConvert.DeserializeObject<T[]>(json);
        }
        catch
        {
            return Array.Empty<T>();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var json = "json";
        modelBuilder.Entity<DeveloperAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasOne(x => x.UserData)
                .WithOne()
                .HasForeignKey<DeveloperAccount>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserData>((e) => { e.HasKey(x => x.Id); });
        modelBuilder.Entity<CharacterData>((e) =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasOne(x => x.User)
                .WithMany(x => x.CharacterDatas)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Progression>(e =>
        {
            e.HasKey(x => x.CharId);

            e.HasOne(x => x.CharacterData)
                .WithOne(x => x.Progression)
                .HasForeignKey<Progression>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Appearance>(e =>
        {
            e.HasKey(x => x.CharId);
            e.Property(x => x.Skins)
                .HasColumnType(json)
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<Dictionary<string, string>>(v)
                )
                .Metadata.SetValueComparer(ValueComparerDict<string, string>.Comparer());

            e.Property(x => x.Colors)
                .HasColumnType(json)
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeObject<Dictionary<string, Color>>(v)
                )
                .Metadata.SetValueComparer(ValueComparerDict<string, Color>.Comparer());
            e.HasOne(x => x.CharacterData)
                .WithOne(x => x.Appearance)
                .HasForeignKey<Appearance>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CharacterStats>(e =>
        {
            e.HasKey(x => x.CharId);
            e.Property(x => x.TuoiTac)
                .HasConversion(
                    v => v.Ticks,
                    v => new TimeSpan(v)
                )
                .Metadata.SetValueComparer(
                    new ValueComparer<TimeSpan>(
                        (a, b) => a == b,
                        v => v.GetHashCode(),
                        v => new TimeSpan(v.Ticks)
                    )
                );
            e.HasOne(x => x.CharacterData)
                .WithOne(x => x.Stats)
                .HasForeignKey<CharacterStats>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            ;
        });
        modelBuilder.Entity<InventoryManager>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharacterData)
                .WithOne(x => x.InventoryManager)
                .HasForeignKey<InventoryManager>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ItemRef>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id, x.Type });
            e.HasOne(x => x.Inventory)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FurnaceData>(e =>
        {
            e.HasKey(x => new { x.CharId, x.SelfId, x.Id });
            e.HasOne(x => x.Inventory)
                .WithMany(x => x.Furnaces)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Mail>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Rewards)
                    .HasColumnType(json)
                    .HasConversion(
                        v => SerializeObject(v),
                        v => DeserializeObject<List<ItemRef>>(v)
                    )
                    .Metadata.SetValueComparer(ValueComparerList<ItemRef>.Comparer());
                e.HasOne(x => x.Social)
                    .WithMany(x => x.Mails)
                    .HasForeignKey(x => x.CharId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        );
        modelBuilder.Entity<Social>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharacterData)
                .WithOne(x => x.Social)
                .HasForeignKey<Social>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Relationship>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Npc });
            e.HasOne(x => x.Social)
                .WithMany(x => x.Relationships)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CamNgo>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne(x => x.Inventory)
                .WithMany(x => x.CamNgos)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CongPhap>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne(x => x.Inventory)
                .WithMany(x => x.CongPhapOwners)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<NgoDao>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne(x => x.Inventory)
                .WithMany(x => x.NgoDaos)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ThanThong>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne(x => x.Inventory)
                .WithMany(x => x.ThanThongOwners)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EquipmentManager>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharacterData)
                .WithOne(x => x.EquipmentManager)
                .HasForeignKey<EquipmentManager>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ThanThongEquip>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne(x => x.EquipmentManager)
                .WithMany(x => x.ThanThongEquips)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DanCombatUsed>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne(x => x.EquipmentManager)
                .WithMany(x => x.DanCombatUseds)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Equipment>(e =>
        {
            e.HasKey(x => new { x.CharId, x.SelfId, x.Id });
            e.HasOne(x => x.Inventory)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Effects)
                .HasColumnType(json)
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeArray<Effect>(v)
                );
            e.Property(x => x.MaterialIds)
                .HasColumnType(json)
                .HasConversion(
                    v => SerializeObject(v),
                    v => DeserializeArray<ushort>(v)
                );
        });
        modelBuilder.Entity<QuestManager>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharData)
                .WithOne(x => x.QuestManager)
                .HasForeignKey<QuestManager>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}