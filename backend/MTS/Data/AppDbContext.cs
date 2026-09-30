using MemoryPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MTS.Game.Auction;
using MTS.Game.BaseSkill;
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
    public DbSet<MTS.Game.Boss.BossProgress> BossProgresses { get; set; }
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
    public DbSet<LocalAccount> LocalAccounts { get; private set; }
    public DbSet<Order> Orders { get; private set; }
    public DbSet<Giftcode> GiftCodes { get; private set; }
    public DbSet<GiftcodeUsed> GiftcodeUseds { get; private set; }
    public DbSet<Promotion> Promotions { get; private set; }
    public DbSet<WelcomeReward> WelcomeRewards { get; private set; }
    public DbSet<ProductLinker> ProductLinkers { get; private set; }
    public DbSet<DailyRewardMonthlyPass> DailyRewardMonthlyPasses { get; private set; }
    public DbSet<UseDan> UseDans { get; private set; }
    public DbSet<MonsterKill> MonsterKills { get; private set; }
    public DbSet<MonsterRankReward> MonsterRankRewards { get; private set; }
    public DbSet<DongPhu> DongPhus { get; private set; }
    public DbSet<PlantGarden> PlantGardens { get; private set; }
    public DbSet<DongPhuInventory> DongPhuInventories { get; private set; }
    public DbSet<DongPhuHistory> DongPhuHistories { get; private set; }
    public DbSet<DongPhuShop> DongPhuShops { get; private set; }
    public DbSet<PlantingSlot> PlantingSlots { get; private set; }
    public DbSet<PlantSeed> PlantSeeds { get; private set; }
    public DbSet<SeedItemShop> SeedItemShops { get; private set; }
    public DbSet<Seed> Seeds { get; private set;  }
    public DbSet<DongPhuLike> DongPhuLikes { get; private set; }
    public DbSet<EventManager> EventManagers { get; private set; }
    public DbSet<Event> Events { get; private set; }
    public DbSet<EventInventory> EventInventories { get; private set; }
    public DbSet<ItemEvent> ItemEvents { get; private set; }
    public DbSet<Clan> Clans { get; private set; }
    public DbSet<ClanMember> ClanMembers { get; private set; }
    public DbSet<ClanJoinRequest> ClanJoinRequests { get; private set; }
    public DbSet<ClanShopItem> ClanShopItems { get; private set; }
    public DbSet<ClanInheritanceEffect> ClanInheritanceEffects { get; private set; }
    public DbSet<ClanNode> ClanNodes { get; private set; }
    public DbSet<Puppet> Puppets { get; private set; }
    public DbSet<WarParticipant> WarParticipants { get; private set; }
    public DbSet<WarBattle> WarBattles { get; private set; }
    public DbSet<WarSettlement> WarSettlements { get; private set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public static AppDbContext FromConnection(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        return new AppDbContext(options.Options);
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
        modelBuilder.Entity<MTS.Game.Boss.BossProgress>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.BossBytes).HasColumnType("longblob");
            e.Property(x => x.ParticipantRewards).HasColumnType("blob");
            e.Property(x => x.KillerRewards).HasColumnType("blob");
            e.Property(x => x.AttemptsJson).HasColumnType("longtext");
            e.Property(x => x.AttemptDaysJson).HasColumnType("longtext");
            e.Property(x => x.DamageJson).HasColumnType("longtext");
        });
        var json = "json";
        var blob = "blob";
        modelBuilder.Entity<GuestAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
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
            e.HasIndex(x => x.Name)
                .IsUnique();
            e.Property(x => x.Name)
                .HasMaxLength(50)
                .UseCollation("utf8mb4_0900_as_cs");
            e.HasOne(x => x.User)
                .WithMany(x => x.CharacterDatas)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Clan>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.NormalizeName).IsUnique();
            e.HasIndex(x => x.NodeId).IsUnique();
            e.Property(x => x.Name).HasMaxLength(15);
            e.Property(x => x.NormalizeName).HasMaxLength(15);
            e.Property(x => x.Description).HasMaxLength(512);
            e.Property(x => x.Announcement).HasMaxLength(512);
            e.Property(x => x.ImageUrl).HasMaxLength(512);
            e.HasMany(x => x.Members)
                .WithOne()
                .HasForeignKey(x => x.ClanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.JoinRequests)
                .WithOne()
                .HasForeignKey(x => x.ClanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.ShopItems)
                .WithOne()
                .HasForeignKey(x => x.ClanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.InheritanceEffects)
                .WithOne(x => x.Clan)
                .HasForeignKey(x => x.ClanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClanMember>(e =>
        {
            e.HasKey(x => new { x.ClanId, x.CharId });
            e.HasIndex(x => x.CharId).IsUnique();
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClanJoinRequest>(e =>
        {
            e.HasKey(x => new { x.ClanId, x.CharId });
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClanShopItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClanId);
            e.HasIndex(x => x.OwnerCharId);
            e.Ignore(x => x.Item);
            e.HasOne<CharacterData>()
                .WithMany()
                .HasForeignKey(x => x.OwnerCharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ClanInheritanceEffect>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ClanId, x.Trigger, x.SlotIndex }).IsUnique();
        });
        // Tông môn chiến: không khai báo navigation tới Clan để tránh load/serialize kèm Clan.
        modelBuilder.Entity<ClanNode>(e =>
        {
            e.HasKey(x => x.NodeId);
            e.Property(x => x.NodeId).ValueGeneratedNever();
            e.HasIndex(x => x.ClanId);
            e.HasOne<Clan>()
                .WithMany()
                .HasForeignKey(x => x.ClanId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Puppet>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ClanId);
            e.Property(x => x.Route).HasMaxLength(1024);
            e.HasOne<Clan>()
                .WithMany()
                .HasForeignKey(x => x.ClanId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<WarParticipant>(e =>
        {
            e.HasKey(x => x.CharId);
            e.Property(x => x.CharId).ValueGeneratedNever();
            e.HasIndex(x => x.ClanId);
            e.Property(x => x.Route).HasMaxLength(1024);
            e.HasOne<CharacterData>()
                .WithOne()
                .HasForeignKey<WarParticipant>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<WarBattle>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.HasIndex(x => x.State);
            e.HasIndex(x => x.NodeId);
        });
        modelBuilder.Entity<WarSettlement>(e =>
        {
            e.HasKey(x => new { x.Kind, x.PeriodStart });
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
            e.HasOne<InventoryManager>()
                .WithMany(x => x.ItemRefs)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FurnaceData>(e =>
        {
            e.HasKey(x => new { x.CharId, x.SelfId, x.Id });
            e.HasOne<InventoryManager>()
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
                e.HasOne<Social>()
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
            e.HasOne<Social>()
                .WithMany(x => x.Relationships)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CamNgo>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne<InventoryManager>()
                .WithMany(x => x.CamNgos)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CongPhap>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne<InventoryManager>()
                .WithMany(x => x.CongPhapOwners)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<NgoDao>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne<InventoryManager>()
                .WithMany(x => x.NgoDaos)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ThanThong>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne<InventoryManager>()
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
            e.HasOne(x => x.Linker)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.LinkerId)
                .OnDelete(DeleteBehavior.SetNull);
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
            e.Property(x => x.PurchaseTokenHash)
                .HasMaxLength(32);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.SetNull);
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
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.GachaThanThongDict)
                .HasColumnType(blob)
                .HasConversion(x => MemoryPackSerializer.Serialize(x),
                    x => ParseDict(x));
            e.Property(x => x.GachaCongPhapDict)
                .HasColumnType(blob)
                .HasConversion(x => MemoryPackSerializer.Serialize(x),
                    x => ParseDict(x));
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

        modelBuilder.Entity<LocalAccount>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username)
                .HasMaxLength(50)
                .UseCollation("utf8mb4_bin");
            e.HasOne(x => x.UserData)
                .WithOne()
                .HasForeignKey<LocalAccount>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.OrderId);
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId);
            e.HasOne(x => x.Purchase)
                .WithOne()
                .HasForeignKey<Order>(x => x.PurchaseId);
        });
        modelBuilder.Entity<Giftcode>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Rewards)
                .HasColumnType(blob)
                .HasConversion(x => MemoryPackSerializer.Serialize(x),
                    x => ParseMailRewards(x));
        });
        modelBuilder.Entity<GiftcodeUsed>(e =>
        {
            e.HasKey(x => new { x.CharId, x.CodeId });
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Giftcode)
                .WithMany()
                .HasForeignKey(x => x.CodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ReleaseDailyRewardClaim>(e =>
        {
            e.HasKey(x => new { x.CharId, x.RewardDay });
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Promotion>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PromotionApplicable)
                .HasColumnType(blob)
                .HasConversion(x => MemoryPackSerializer.Serialize(x),
                    x => ParsePromotionApplicable(x));
        });
        modelBuilder.Entity<WelcomeReward>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharData)
                .WithOne()
                .HasForeignKey<WelcomeReward>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ProductLinker>(e => { e.HasKey(x => x.Id); });
        modelBuilder.Entity<DailyRewardMonthlyPass>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.CharData)
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Rewards)
                .HasColumnType(blob)
                .HasConversion(
                    v => MemoryPackSerializer.Serialize(v),
                    v => MemoryPackSerializer.Deserialize<List<IMailReward>>(v)
                );
        });
        modelBuilder.Entity<UseDan>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne<CharacterData>()
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MonsterKill>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne(x => x.CharData)
                .WithOne(x => x.MonsterKill)
                .HasForeignKey<MonsterKill>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MonsterRankReward>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.MonsterKill)
                .WithMany(x => x.Rewards)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Rewards)
                .HasColumnType(blob)
                .HasConversion(x =>  MemoryPackSerializer.Serialize(x),
                    x => ParseMonsterRankRewards(x));
        });
        modelBuilder.Entity<DongPhu>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne<CharacterData>()
                .WithOne()
                .HasForeignKey<DongPhu>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DongPhuShop>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne<DongPhu>()
                .WithOne(x => x.Shop)
                .HasForeignKey<DongPhuShop>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DongPhuInventory>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne<DongPhu>()
              .WithOne(x => x.Inventory)
              .HasForeignKey<DongPhuInventory>(x => x.CharId)
              .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DongPhuHistory>(e =>
        {
            e.HasOne<DongPhu>()
                .WithMany()
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlantGarden>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne<DongPhu>()
              .WithOne(x => x.Garden)
              .HasForeignKey<PlantGarden>(x => x.CharId)
              .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlantingSlot>(e =>
        {
            e.HasKey(x => new { x.CharId, x.SlotIndex });

            e.HasOne<PlantGarden>()
            .WithMany(x => x.Slots)
            .HasForeignKey(x => x.CharId)
            .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PlantSeed>(e =>
        {
            e.HasKey(x => new { x.CharId, x.SlotIndex });

            e.Property(x => x.WateringHelps)
            .HasColumnType(blob)
            .HasConversion(x => MemoryPackSerializer.Serialize(x),
            x => ParseWateringHelps(x));

            e.HasOne<PlantingSlot>()
                .WithOne()
                .HasForeignKey<PlantSeed>(x => new
                {
                    x.CharId,
                    x.SlotIndex
                })
                .HasPrincipalKey<PlantingSlot>(x => new
                {
                    x.CharId,
                    x.SlotIndex
                })
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<PlantGarden>()
             .WithMany(x => x.Seeds)
             .HasForeignKey(x => x.CharId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SeedItemShop>(e =>
        {
            e.HasKey(x => new { x.CharId, x.HerbId });
            e.HasOne<DongPhuShop>()
            .WithMany(x => x.Seeds)
            .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Seed>(e =>
        {
            e.HasKey(x => new { x.CharId, x.HerbId });
            e.HasOne<DongPhuInventory>()
            .WithMany(x => x.Seeds)
            .HasForeignKey(x => x.CharId)
            .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DongPhuLike>(e =>
        {
            e.HasKey(x => new { x.OwnerCharId, x.OtherCharId });
            e.HasOne<DongPhu>()
                .WithMany(x => x.Likes)
                .HasForeignKey(x => x.OwnerCharId)
                .HasPrincipalKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventManager>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne<CharacterData>()
                .WithOne(x => x.EventManager)
                .HasForeignKey<EventManager>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<EventInventory>(e =>
        {
            e.HasKey(x => x.CharId);
            e.HasOne<EventManager>()
                .WithOne(x => x.Inventory)
                .HasForeignKey<EventInventory>(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ItemEvent>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne<EventInventory>()
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(x => new { x.CharId, x.Id });
            e.HasOne<EventManager>()
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.CharId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.GameEvent)
                .HasColumnType(blob)
                .HasConversion(x => MemoryPackSerializer.Serialize(x),
                    x => MemoryPackSerializer.Deserialize<IGameEvent>(x));
        });
        
    }
    private List<WateringHelp> ParseWateringHelps(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return new List<WateringHelp>();
        }
        return MemoryPackSerializer.Deserialize<List<WateringHelp>>(data);
    } 
    private Dictionary<ETypeSkill, int> ParseDict(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return new Dictionary<ETypeSkill, int>();
        }
        return MemoryPackSerializer.Deserialize<Dictionary<ETypeSkill, int>>(data);
    }

    private List<IEventReward> ParseMonsterRankRewards(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }
        return MemoryPackSerializer.Deserialize<List<IEventReward>>(data);
    }
    private IPromotionApplicable ParsePromotionApplicable(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        return MemoryPackSerializer.Deserialize<IPromotionApplicable>(data);
    }

    private List<IMailReward> ParseMailRewards(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return null;
        }

        return MemoryPackSerializer.Deserialize<List<IMailReward>>(data);
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
