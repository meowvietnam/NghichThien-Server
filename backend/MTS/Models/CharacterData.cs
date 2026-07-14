using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.ItemProvider;
using MTS.Game.LuyenDan;
using MTS.Game.Resources;
using MTS.Game.Social;
using MTS.Localization;
using MTS.Network.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.IAP;
using MTS.Game.Combat;
using MTS.Network;

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

    public void SetCurrentFreeAdsMoneyVipCount(int value)
    {
        if (CurrentFreeAdsMoneyVipCount != value)
        {
            CurrentFreeAdsMoneyVipCount = value;
            dirty = true;
        }
    }

    public CharacterData()
    {
    }

    public CharacterData(CharacterData clone)
    {
        Clone1(clone);
    }

    private void Clone0(CharacterData clone)
    {
        MonPhai = clone.MonPhai;
        MoneyVip = clone.MoneyVip;
        IsInfiniteEnergy = clone.IsInfiniteEnergy;
        IsNoAds = clone.IsNoAds;
        CanFly = clone.CanFly;
        CurrentFreeAdsMoneyVipCount = clone.CurrentFreeAdsMoneyVipCount;
        CreatedAt = clone.CreatedAt;
        dirty = false;
    }

    private void Clone1(CharacterData clone)
    {
        Id = clone.Id;
        Gender = clone.Gender;
        Name = clone.Name;
        Stats.CopyFrom(clone.Stats);
        InventoryManager.Clone(clone.InventoryManager);
        CreatedAt = clone.CreatedAt;
        Progression.CopyFrom(clone.Progression);
        Appearance.CopyFrom(clone.Appearance);
        Social.CopyFrom(clone.Social);
        EquipmentManager.CopyFrom(clone.EquipmentManager);
        QuestManager.CopyFrom(clone.QuestManager);
        ArenaManager.CopyFrom(clone.ArenaManager);
        ShopManager.CopyFrom(clone.ShopManager);
        Clone0(clone);
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
               + ShopManager.ByteSize();
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
    }

    public bool IsInventoryDefault()
    {
        return InventoryManager.IsDefault();
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
                        .SetProperty(x => x.CurrentFreeAdsMoneyVipCount, CurrentFreeAdsMoneyVipCount));
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
    }

}
