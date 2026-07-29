using MemoryPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MTS.Game.Auction;
using MTS.Game.LuyenKhi;
using MTS.IAP;
using MTS.Models;
using MTS.Network.Models;
using MTS.Utils;
using Newtonsoft.Json;

namespace MTS.Data;

public class AppDbContext : DbContext
{
    public DbSet<GuestAccount> GuestAccounts { get; set; }
    public DbSet<GoogleAccount> GoogleAccounts { get; set; }
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
    public DbSet<IapProduct> IapProducts { get; private set; }
    public DbSet<IapPurchase> IapPurchases { get; private set; }
    public DbSet<ArenaManager> ArenaManagers { get; private set; }
    public DbSet<ShopManager> ShopManagers { get; private set; }
    public DbSet<DailyQuest> DailyQuests { get; private set; }
    public DbSet<RankReward> RankRewards { get; private set; }
    public DbSet<SpendingMoneyVip> SpendingMoneyVips { get; private set; }
    public DbSet<GachaManager> GachaManagers { get; private set; }
    public DbSet<AuctionHistory> AuctionHistories { get; private set; }
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
        var blob = "blob";
        modelBuilder.Entity<GuestAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.UserName).IsUnique();
            e.HasOne(x => x.UserData)
                .WithOne()
                .HasForeignKey<GuestAccount>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GoogleAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.UserData)
                .WithOne()
                .HasForeignKey<GoogleAccount>(x => x.UserId)
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
                .WithMany(x => x.ItemRefs)
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
                    .HasColumnType(blob)
                    .HasConversion(
                        v => MemoryPackSerializer.Serialize(v),
                        v => MemoryPackSerializer.Deserialize<List<IMailReward>>(v)
                    );
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

        modelBuilder.Entity<IapProduct>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.StoreProductId, x.Store })
                .IsUnique();
            e.Property(x => x.Reward)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => MemoryPackSerializer.Deserialize<IIapReward>(v)
                );
        });
        modelBuilder.Entity<IapPurchase>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Store, x.PurchaseTokenHash })
                .IsUnique();
            e.Property(x => x.PurchaseTokenHash)
                .HasMaxLength(32);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ArenaManager>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharData)
                .WithOne(x => x.ArenaManager)
                .HasForeignKey<ArenaManager>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ShopManager>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharData)
                .WithOne(x => x.ShopManager)
                .HasForeignKey<ShopManager>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.ShopDuocLieu)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => ParseShop(v)
                );
            e.Property(x => x.ShopVatLieu)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => ParseShop(v)
                );
            e.Property(x => x.ShopSkill)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => ParseShop(v)
                );
            e.Property(x => x.ShopSkillAll)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => ParseShop(v)
                );
        });
        modelBuilder.Entity<DailyQuest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.QuestReward)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => MemoryPackSerializer.Deserialize<IQuestReward>(v)
                );
            e.HasOne(x => x.QuestManager)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RankReward>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.ArenaManager)
                .WithMany(x => x.Rewards)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SpendingMoneyVip>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<GachaManager>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharData)
                .WithOne(x => x.GachaManager)
                .HasForeignKey<GachaManager>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);;
        });
        modelBuilder.Entity<AuctionHistory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Sessions)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => ParseSessions(v)
                );
        });
    }

    private AuctionSession[] ParseSessions(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }
        return MemoryPackSerializer.Deserialize<AuctionSession[]>(data);
    }
    private Shop ParseShop(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }
        return MemoryPackSerializer.Deserialize<Shop>(data);
    }
}