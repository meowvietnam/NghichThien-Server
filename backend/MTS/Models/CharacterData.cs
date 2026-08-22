using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Network.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.Exceptions;
using MTS.Game.CreateCharacter;
using MTS.Game.LuyenDan;
using MTS.ND;
using MTS.Network;
using MTS.Services;
using MTS.Utils;
using Network.Extensions.MemoryPack;
using Serilog;

namespace MTS.Models;

public partial class CharacterData : IMemorySerializable<CharacterData>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int UserId { get; set; }

    [Key] public int Id { get; set; }
    public Gender Gender { get; set; }
    public string Name { get; set; }
    public EMonPhai MonPhai { get; private set; }
    public int MoneyVip { get; private set; } = 0;
    public bool IsInfiniteEnergy { get; private set; } = false;
    public bool IsNoAds { get; private set; } = false;
    public bool CanFly { get; private set; } = false;
    public int CurrentFreeAdsMoneyVipCount { get; private set; }
    public int TrungSinhCount { get; private set; } = 0;
    public int BonusPoint { get; private set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public CharacterStats Stats { get; set; } = new();
    public Progression Progression { get; set; } = new();
    public Appearance Appearance { get; set; } = new();
    public EquipmentManager EquipmentManager { get; set; } = new();
    public Social Social { get; set; } = new();
    public InventoryManager InventoryManager { get; set; } = new();
    public QuestManager QuestManager { get; set; } = new();
    public ArenaManager ArenaManager { get; set; } = new();
    public ShopManager ShopManager { get; set; } = new();
    public GachaManager GachaManager { get; set; } = new();
    public MonsterKill MonsterKill { get; set; } = new();
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public UserData User { get; set; }


    [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private bool dirty;

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    [NotMapped]
    public Client? Client { get; set; }

    [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private List<SpendingMoneyVip> addedSpendingMoneyVips = new();

    [JsonIgnore] [Newtonsoft.Json.JsonIgnore] [MemoryPackIgnore] [NotMapped]
    private List<UseDan> useDans = new();

    public void SetMonPhai(EMonPhai value)
    {
        if (MonPhai != value)
        {
            MonPhai = value;
            dirty = true;
        }
    }

    public void SetNoAds(bool value)
    {
        if (IsNoAds != value)
        {
            IsNoAds = value;
            dirty = true;
        }
    }

    public void SetMoneyVip(int value)
    {
        if (MoneyVip != value)
        {
            MoneyVip = value;
            dirty = true;
        }
    }

    public void SetIsInfiniteEnergy(bool value)
    {
        if (IsInfiniteEnergy != value)
        {
            IsInfiniteEnergy = value;
            dirty = true;
        }
    }

    public void SetCanFly(bool value)
    {
        if (CanFly != value)
        {
            CanFly = value;
            dirty = true;
        }
    }

    public void SetTrungSinhCount(int value)
    {
        if (TrungSinhCount != value)
        {
            TrungSinhCount = value;
            dirty = true;
        }
    }

    public void SetCurrentFreeAdsMoneyVipCount(int value)
    {
        if (CurrentFreeAdsMoneyVipCount != value)
        {
            CurrentFreeAdsMoneyVipCount = value;
            dirty = true;
        }
    }

    public static CharacterData DeepClone(CharacterData clone)
    {
        var writer = new Writer(clone.ByteSize());
        writer.Write(clone);
        var reader = new Reader(writer.Buffer);
        return reader.Read<CharacterData>();
    }

    public int ByteSize()
    {
        return Id.ByteSize()
               + Gender.ByteSize()
               + Name.ByteSize()
               + Stats.ByteSize()
               + Progression.ByteSize()
               + Appearance.ByteSize()
               + EquipmentManager.ByteSize()
               + MonPhai.ByteSize()
               + MoneyVip.ByteSize()
               + IsInfiniteEnergy.ByteSize()
               + IsNoAds.ByteSize()
               + CanFly.ByteSize()
               + CurrentFreeAdsMoneyVipCount.ByteSize()
               + InventoryManager.ByteSize()
               + CreatedAt.ByteSize()
               + Social.ByteSize()
               + QuestManager.ByteSize()
               + ArenaManager.ByteSize()
               + ShopManager.ByteSize()
               + GachaManager.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Gender);
        writer.Write(Name);
        writer.Write(Stats);
        writer.Write(Progression);
        writer.Write(Appearance);
        writer.Write(EquipmentManager);
        writer.Write(MonPhai);
        writer.Write(MoneyVip);
        writer.Write(IsInfiniteEnergy);
        writer.Write(IsNoAds);
        writer.Write(CanFly);
        writer.Write(CurrentFreeAdsMoneyVipCount);
        writer.Write(InventoryManager);
        writer.Write(CreatedAt);
        writer.Write(Social);
        writer.Write(QuestManager);
        writer.Write(ArenaManager);
        writer.Write(ShopManager);
        writer.Write(GachaManager);
    }

    public void Deserialize(IReader reader, ref CharacterData value)
    {
        value.Id = reader.Read<int>();
        value.Gender = reader.Read<Gender>();
        value.Name = reader.Read();
        value.Stats = reader.Read<CharacterStats>();
        value.Progression = reader.Read<Progression>();
        value.Appearance = reader.Read<Appearance>();
        value.EquipmentManager = reader.Read<EquipmentManager>();
        value.MonPhai = reader.Read<EMonPhai>();
        value.MoneyVip = reader.Read<int>();
        value.IsInfiniteEnergy = reader.Read<bool>();
        value.IsNoAds = reader.Read<bool>();
        value.CanFly = reader.Read<bool>();
        value.CurrentFreeAdsMoneyVipCount = reader.Read<int>();
        value.InventoryManager = reader.Read<InventoryManager>();
        value.CreatedAt = reader.Read<DateTime>();
        value.Social = reader.Read<Social>();
        value.QuestManager = reader.Read<QuestManager>();
        value.ArenaManager = reader.Read<ArenaManager>();
        value.ShopManager = reader.Read<ShopManager>();
        value.GachaManager = reader.Read<GachaManager>();
    }

    public bool IsInventoryDefault()
    {
        return InventoryManager.IsDefault();
    }

    public void SetBonusPoints(int bonusPoints)
    {
        if (BonusPoint != bonusPoints)
        {
            BonusPoint = bonusPoints;
            dirty = true;
        }
    }

    public TrungSinhConfig GetTrungSinhConfig()
    {
        var infoLevels = GameConfig.LevelResource.infoLevels;
        var level = Progression.Level;
        var levels = infoLevels.Take(level - 1).ToArray();
        var daiCanhGioi = levels.Count(x => x.IsSpecialLevelUp);
        var prices = GameConfig.TrungSinhPrices;
        var index = Math.Clamp(TrungSinhCount, 0, prices.Length - 1);
        var price = prices[index];
        var thienPhu = GameConfig.OriginalMoney + BonusPoint + daiCanhGioi * daiCanhGioi;
        var stats = GetResetStats();
        return new()
        {
            DaiCanhGioi = daiCanhGioi,
            MoneyVip = price,
            ThienPhu = thienPhu,
            TargetLevel = 1,
            Stats = stats
        };
    }

    public async Task TrungSinh(List<SelectionModel> selections)
    {
        var config = GetTrungSinhConfig();
        var remaining = MoneyVip - config.MoneyVip;
        if (remaining < 0)
        {
            throw new InvalidException(string.Empty);
        }

        var service = new CreateCharacterService(config.ThienPhu, GameConfig.StepConfigTrungSinh);
        service.ApplyStats(selections, config.Stats);
        var newStats = service.Char.Stats;

        var ability = newStats.Ability;
        if (ability.HasFlag(Ability.TeNuiKyNgo))
        {
            var herbs = GameConfig.LuyenDanResources.HerbDatas;
            var herbIds = new[] { 2067, 2077, 2073, 2039 };
            var items = herbIds.Select(x => herbs.First(i => i.Id == x))
                .Select(x => new ItemRef()
                {
                    Id = (ushort)x.Id,
                    Type = ItemType.Herb,
                    Count = 1
                })
                .ToList();
            InventoryManager.AddToInventory(items);
        }

        var congPhapId = ability.HasFlag(Ability.GiaTocTuChan) ? (ushort)536 : (ushort)501;
        var congPhap = GameConfig.NDCPTTManager.CongPhaps.FirstOrDefault(x => x.Id == congPhapId);
        var congPhapRef = new CongPhap()
        {
            Id = congPhapId,
            Level = 1,
        };
        if (!InventoryManager.CongPhapOwners.Contains(congPhapRef))
        {
            InventoryManager.Add(congPhapRef);
        }

        SetTrungSinhCount(TrungSinhCount + 1);
        Progression.ResetLevel(config.TargetLevel);
        Stats.CopyFrom(newStats);
        SetMoneyVip(remaining);
        SetBonusPoints(BonusPoint + config.DaiCanhGioi * config.DaiCanhGioi);
        AddSpendMoneyVipOnline(config.MoneyVip, SpendFor.TrungSinh);
        await using var db = await Program.CreateDbAsync();
        await RefundLinhDan(db);
    }

    public void AddUseDan(ushort danId, int count)
    {
        var useDan = new UseDan()
        {
            DanId = danId,
            CharId = Id,
            Count = count
        };
        useDans.Add(useDan);
    }

    public CharacterData DeepClone()
    {
        var writer = new Writer(ByteSize());
        writer.Write(this);
        var reader = new Reader(writer.Buffer);
        return reader.Read<CharacterData>();
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (dirty)
        {
            await db.Characters
                .Where(x => x.Id == Id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.MonPhai, MonPhai)
                        .SetProperty(x => x.MoneyVip, MoneyVip)
                        .SetProperty(x => x.IsInfiniteEnergy, IsInfiniteEnergy)
                        .SetProperty(x => x.IsNoAds, IsNoAds)
                        .SetProperty(x => x.CanFly, CanFly)
                        .SetProperty(x => x.CurrentFreeAdsMoneyVipCount, CurrentFreeAdsMoneyVipCount)
                        .SetProperty(x => x.TrungSinhCount, TrungSinhCount)
                        .SetProperty(x => x.BonusPoint, BonusPoint));
            dirty = false;
        }

        lock (addedSpendingMoneyVips)
        {
            if (addedSpendingMoneyVips.Count > 0)
            {
                db.SpendingMoneyVips.AddRange(addedSpendingMoneyVips);
                addedSpendingMoneyVips.Clear();
            }
        }

        lock (useDans)
        {
            if (useDans.Count > 0)
            {
                db.UseDans.AddRange(useDans);
                useDans.Clear();
            }
        }

        if (!Stats.IsDefault())
        {
            await Stats.SaveChanges(db);
        }

        if (!Progression.IsDefault())
        {
            await Progression.SaveChanges(db);
        }

        if (!EquipmentManager.IsDefault())
        {
            await EquipmentManager.SaveChanges(db);
        }

        if (!Social.IsDefault())
        {
            await Social.SaveChanges(db);
        }

        if (!InventoryManager.IsDefault())
        {
            await InventoryManager.SaveChanges(db);
        }

        if (!QuestManager.IsDefault())
        {
            await QuestManager.SaveChanges(db);
        }

        if (!ArenaManager.IsDefault())
        {
            await ArenaManager.SaveChanges(db);
        }

        if (!ShopManager.IsDefault())
        {
            await ShopManager.SaveChanges(db);
        }

        if (!GachaManager.IsDefault())
        {
            await GachaManager.SaveChanges(db);
        }

        if (!MonsterKill.IsDefault())
        {
            await MonsterKill.SaveChanges(db);
        }
    }

    public void AcceptChanges()
    {
        if (!InventoryManager.IsDefault())
        {
            InventoryManager.AcceptChanges();
        }
    }

    public void ApplyReset(DailyReset dailyReset)
    {
        ArenaManager.SetCurrentArenaCount(dailyReset.CurrentArenaCount);
        QuestManager.SetCurrentCountSideQuest(dailyReset.CurrentCountSideQuest);
    }

    public async Task UpdateLogin()
    {
        LastLogin = DateTime.UtcNow;
        await using var db = await Program.CreateDbAsync();
        await db.Characters
            .Where(x => x.Id == Id)
            .ExecuteUpdateAsync(x => x.SetProperty(u => u.LastLogin, LastLogin));
    }

    public void TryResetAds()
    {
        if (Utils.Utils.IsToday(LastLogin))
        {
            return;
        }

        SetCurrentFreeAdsMoneyVipCount(0);
    }


    public CharacterStats GetResetStats()
    {
        var stats = Stats.Clone();
        var statsTemp = GameConfig.CharacterTemplate.Stats.Clone();
        var hp = statsTemp.SinhMenh;
        stats.SetSinhMenh(hp);
        stats.SetHp(hp);
        stats.SetThanThuc(statsTemp.ThanThuc);
        stats.SetTocDoBay(statsTemp.TocDoBay);
        stats.SetThoNguyen(100);
        stats.SetLinhCan(LinhCan.NguLinhCan);
        stats.SetDanDoc(0);
        stats.ResetLinhCan();
        stats.ResetTuoiTac();
        stats.ResetAbility();
        stats.ResetTuChat();
        stats.ResetNgoTinh();
        stats.ResetTamCanh();
        stats.SetMa(0);
        return stats;
    }
    public async Task RefundLinhDan(AppDbContext db)
    {
        ETypeDan[] returnTypeDans = [ETypeDan.DanBuff , ETypeDan.DanDotPha];
        EDanBuffType[] exclusives = [
            EDanBuffType.TuVi,
            EDanBuffType.TuViVaDanDocDuoiTrucCo,
            EDanBuffType.TuViVaDanDocDuoiKetDan,
            EDanBuffType.TuViVaDanDocDuoiNguyenAnh,
            EDanBuffType.TuViVaDanDocDuoiHoaThan,
            EDanBuffType.NgoDaoDiem,
            EDanBuffType.KimChiDao,
            EDanBuffType.MocChiDao,
            EDanBuffType.ThuyChiDao,
            EDanBuffType.HoaChiDao,
            EDanBuffType.ThoChiDao,
            EDanBuffType.ThanThucChiDao,
            EDanBuffType.KiemChiDao,
            EDanBuffType.DanDoc,
            EDanBuffType.TamCanh,
            EDanBuffType.Hp,
            EDanBuffType.HoiFullHp,
        ];
        var linhDans = GameConfig.LuyenDanResources.LinhDans;
        var danIds = linhDans
            .Where(x => returnTypeDans.Contains(x.ETypeDan) && !exclusives.Contains(x.EBuffType))
            .Select(x => x.Id)
            .ToList();
        var danUseds = await db.UseDans
            .Where(x => x.CharId == Id && danIds.Contains(x.DanId))
            .ToListAsync();
        var refunds = danUseds.GroupBy(x => x.DanId)
            .Select(x => new ItemRef()
            {
                CharId = Id,
                Id = x.Key,
                Count = (ushort)x.Sum(i => i.Count),
                Type = ItemType.LinhDan
            }).ToArray();
        if (!refunds.Any())
        {
            return;
        }

        var mailManager = new MailManager(this, Client);
        db.UseDans.RemoveRange(danUseds);
        var chunks = refunds.Chunk(6).ToArray();
        foreach (var chunk in chunks)
        {
            var rewards = chunk.Select(x => new MailRewardItem()
            {
                Item = x
            } as IMailReward).ToList();
            var mail = new Mail()
            {
                CharId = Id,
                Title = "Hoàn Trả Linh Đan",
                Content = "Hoàn đan trùng sinh.",
                Rewards = rewards
            };
            await mailManager.SendMail(mail,db);
        }
    }
}