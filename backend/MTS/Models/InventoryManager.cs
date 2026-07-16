using System.ComponentModel.DataAnnotations.Schema;
using Google.Apis.AndroidPublisher.v3.Data;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.LuyenDan;
using MTS.Game.LuyenKhi;
using MTS.Game.Resources;
using MTS.Localization;
using MTS.Network.Models;
using MTS.Utils;
using Newtonsoft.Json;

namespace MTS.Models;

public class ItemCollection
{
    public object Collection { get; private set; }
    public object Tracking { get; private set; }

    public ItemCollection(object collection, object tracking)
    {
        Collection = collection;
        Tracking = tracking;
    }
}

public class InventoryManager : IMemorySerializable<InventoryManager>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }

    public HashSet<FurnaceData> Furnaces { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> Recipes { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> Herbs { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> ThanThongs { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> CongPhaps { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> LinhDans { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> Materials { get; set; } = new();
    [NotMapped] public HashSet<Equipment> VuKhis { get; set; } = new();
    [NotMapped] public HashSet<Equipment> PhongCus { get; set; } = new();
    [NotMapped] public HashSet<Equipment> TrangSucs { get; set; } = new();
    public HashSet<NgoDao> NgoDaos { get; set; } = new();
    public HashSet<CamNgo> CamNgos { get; set; } = new();
    public HashSet<CongPhap> CongPhapOwners { get; set; } = new();
    public HashSet<ThanThong> ThanThongOwners { get; set; } = new();

    [NotMapped] [System.Text.Json.Serialization.JsonIgnore] [JsonIgnore]
    private IdAllocator<ushort> furnaceIdAllocator;

    [System.Text.Json.Serialization.JsonIgnore] [JsonIgnore] [NotMapped]
    private IdAllocator<ushort> equipmentIdAllocator;

    [NotMapped]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public InventoryTracking Tracking { get; private set; } = new();

    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public CharacterData CharacterData { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public HashSet<ItemRef> ItemRefs { get; private set; } = new();

    public void Init()
    {
        Tracking = new();
        furnaceIdAllocator = new IdAllocator<ushort>(0, Increase, Decrease);
        equipmentIdAllocator = new IdAllocator<ushort>(0, Increase, Decrease);
        var equipments = VuKhis.Union(PhongCus).Union(TrangSucs).ToHashSet();
        foreach (var furnace in Furnaces)
        {
            furnaceIdAllocator.AddUsedId(furnace.SelfId);
        }

        foreach (var equipment in equipments)
        {
            equipmentIdAllocator.AddUsedId(equipment.SelfId);
        }
    }

    public ItemCollection GetItemCollection(ItemType type)
    {
        switch (type)
        {
            case ItemType.Furnace:
                return new ItemCollection(Furnaces, Tracking.Furnaces);
            case ItemType.Recipe:
                return new ItemCollection(Recipes, Tracking.Items);
            case ItemType.Herb:
                return new ItemCollection(Herbs, Tracking.Items);
            case ItemType.ThanThong:
                return new ItemCollection(ThanThongs, Tracking.Items);
            case ItemType.CongPhap:
                return new ItemCollection(CongPhaps, Tracking.Items);
            case ItemType.LinhDan:
                return new ItemCollection(LinhDans, Tracking.Items);
            case ItemType.NgoDao:
                return new ItemCollection(NgoDaos, Tracking.NgoDaos);
            case ItemType.CamNgo:
                return new ItemCollection(CamNgos, Tracking.CamNgos);
            case ItemType.CongPhapOwner:
                return new ItemCollection(CongPhapOwners, Tracking.CongPhaps);
            case ItemType.ThanThongOwner:
                return new ItemCollection(ThanThongOwners, Tracking.ThanThongs);
            case ItemType.Material:
                return new ItemCollection(Materials, Tracking.Items);
            case ItemType.VuKhi:
                return new ItemCollection(VuKhis, Tracking.Equipments);
            case ItemType.PhongCu:
                return new ItemCollection(PhongCus, Tracking.Equipments);
            case ItemType.TrangSuc:
                return new ItemCollection(TrangSucs, Tracking.Equipments);
            default:
                return null;
        }
    }

    private ushort Increase(ushort x)
    {
        return (ushort)(x + 1);
    }

    private ushort Decrease(ushort x)
    {
        return (ushort)(x - 1);
    }

    public async Task LoadEquipments(AppDbContext db)
    {
        var equipments = await db.Equipments
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .ToArrayAsync();
        VuKhis = equipments.Where(x => x.GetEquipmentType() == EquipmentType.Weapon).ToHashSet();
        PhongCus = equipments.Where(x => x.GetEquipmentType() == EquipmentType.Armor).ToHashSet();
        TrangSucs = equipments.Where(x => x.GetEquipmentType() == EquipmentType.Jewelry).ToHashSet();
    }

    public async Task Load(AppDbContext db)
    {
        var items = await db.Items
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .GroupBy(x => x.Type)
            .ToDictionaryAsync(x => x.Key, x => x.ToHashSet());
        var furnaces = await db.Furnaces
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .ToHashSetAsync();
        Furnaces = furnaces;
        Recipes = items.GetValueOrDefault(ItemType.Recipe, new());
        Herbs = items.GetValueOrDefault(ItemType.Herb, new());
        ThanThongs = items.GetValueOrDefault(ItemType.ThanThong, new());
        CongPhaps = items.GetValueOrDefault(ItemType.CongPhap, new());
        LinhDans = items.GetValueOrDefault(ItemType.LinhDan, new());
        Materials = items.GetValueOrDefault(ItemType.Material, new());
        await LoadEquipments(db);
        NgoDaos = await db.NgoDaos
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .ToHashSetAsync();
        CamNgos = await db.CamNgos
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .ToHashSetAsync();
        CongPhapOwners = await db.CongPhaps
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .ToHashSetAsync();
        ThanThongOwners = await db.ThanThongs
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .ToHashSetAsync();
        Init();
    }

    public void AddFurnace(FurnaceData furnace)
    {
        furnace.CharId = CharId;
        var furnaceId = furnaceIdAllocator.GetId();
        furnace.SelfId = furnaceId;
        Furnaces.Add(furnace);
        Tracking.Furnaces.Add(furnace);
    }

    public void UpdateFurnace(FurnaceData furnaceRef)
    {
        if (Furnaces.TryGetValue(furnaceRef, out var furnace))
        {
            furnace.Durability = furnaceRef.Durability;
            Tracking.Furnaces.Update(furnace);
        }
    }

    public bool RemoveFurnace(FurnaceData furnace)
    {
        if (Furnaces.Remove(furnace))
        {
            Tracking.Furnaces.Delete(furnace);
            return true;
        }

        return false;
    }

    public bool RemoveFurnace(ushort selfId)
    {
        var furnace = new FurnaceData()
        {
            CharId = CharId,
            SelfId = selfId
        };
        return RemoveFurnace(furnace);
    }

    public int ByteSize()
    {
        return Furnaces.ByteSize() + Recipes.ByteSize() + Herbs.ByteSize() + ThanThongs.ByteSize() +
               CongPhaps.ByteSize() + VuKhis.ByteSize() + PhongCus.ByteSize() + TrangSucs.ByteSize() +
               LinhDans.ByteSize() + NgoDaos.ByteSize() + CamNgos.ByteSize() + CongPhapOwners.ByteSize()
               + ThanThongOwners.ByteSize() + Materials.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.WriteArray(Furnaces);
        writer.WriteArray(Recipes);
        writer.WriteArray(Herbs);
        writer.WriteArray(ThanThongs);
        writer.WriteArray(CongPhaps);
        writer.WriteArray(VuKhis);
        writer.WriteArray(PhongCus);
        writer.WriteArray(TrangSucs);
        writer.WriteArray(LinhDans);
        writer.WriteArray(NgoDaos);
        writer.WriteArray(CamNgos);
        writer.WriteArray(CongPhapOwners);
        writer.WriteArray(ThanThongOwners);
        writer.WriteArray(Materials);
    }

    public void Deserialize(IReader reader, ref InventoryManager value)
    {
        value.Furnaces = reader.ReadArray<FurnaceData>().ToHashSet();
        value.Recipes = reader.ReadArray<ItemRef>().ToHashSet();
        value.Herbs = reader.ReadArray<ItemRef>().ToHashSet();
        value.ThanThongs = reader.ReadArray<ItemRef>().ToHashSet();
        value.CongPhaps = reader.ReadArray<ItemRef>().ToHashSet();
        value.VuKhis = reader.ReadArray<Equipment>().ToHashSet();
        value.PhongCus = reader.ReadArray<Equipment>().ToHashSet();
        value.TrangSucs = reader.ReadArray<Equipment>().ToHashSet();
        value.LinhDans = reader.ReadArray<ItemRef>().ToHashSet();
        value.NgoDaos = reader.ReadArray<NgoDao>().ToHashSet();
        value.CamNgos = reader.ReadArray<CamNgo>().ToHashSet();
        value.CongPhapOwners = reader.ReadArray<CongPhap>().ToHashSet();
        value.ThanThongOwners = reader.ReadArray<ThanThong>().ToHashSet();
        value.Materials = reader.ReadArray<ItemRef>().ToHashSet();
    }

    public void Clone(InventoryManager other)
    {
        CharId = other.CharId;
        Furnaces = new HashSet<FurnaceData>(other.Furnaces);
        Recipes = new HashSet<ItemRef>(other.Recipes);
        Herbs = new HashSet<ItemRef>(other.Herbs);
        ThanThongs = new HashSet<ItemRef>(other.ThanThongs);
        CongPhaps = new HashSet<ItemRef>(other.CongPhaps);
        VuKhis = new HashSet<Equipment>(other.VuKhis);
        PhongCus = new HashSet<Equipment>(other.PhongCus);
        TrangSucs = new HashSet<Equipment>(other.TrangSucs);
        LinhDans = new HashSet<ItemRef>(other.LinhDans);
        NgoDaos = new HashSet<NgoDao>(other.NgoDaos);
        CamNgos = new HashSet<CamNgo>(other.CamNgos);
        CongPhapOwners = new HashSet<CongPhap>(other.CongPhapOwners);
        ThanThongOwners = new HashSet<ThanThong>(other.ThanThongOwners);
        furnaceIdAllocator = other.furnaceIdAllocator;
        Tracking = other.Tracking;
        CharacterData = other.CharacterData;
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public Task SaveChanges(AppDbContext db)
    {
        lock (Tracking)
        {
            Tracking.Apply(db);
            Tracking.Clear();
        }

        return Task.CompletedTask;
    }

    public RewardItems AddToInventory(List<ItemRef> rewards)
    {
        var result = new RewardItems();
        foreach (var itemRef in rewards)
        {
            var type = itemRef.Type;
            if (type == ItemType.Furnace)
            {
                var furnace = new FurnaceData()
                {
                    Id = itemRef.Id,
                    Durability = 100,
                };
                result.Furnaces.Add(furnace);
                AddFurnace(furnace);
            }
            else
            {
                result.OtherItems.Add(itemRef);
                Add(itemRef);
            }
        }

        return result;
    }

    public void Decrease(ItemRef itemRef)
    {
        if (itemRef.Count == 0)
        {
            throw new Exception(Strings.InventoryItemQuantityInvalid);
        }

        var collection = GetItemCollection(itemRef.Type);
        if (collection.Collection is HashSet<ItemRef> c)
        {
            if (!c.TryGetValue(itemRef, out var item))
            {
                throw new Exception(Strings.InventoryItemNotFound);
            }

            var remaining = item.Count - itemRef.Count;
            if (remaining < 0)
            {
                throw new Exception(Strings.InventoryItemQuantityInvalid);
            }

            item.Count = (ushort)remaining;
            var tracking = collection.Tracking as Modify<ItemRef>;
            if (remaining == 0)
            {
                c.Remove(item);
                tracking.Delete(item);
            }
            else
            {
                tracking.Update(item);
            }
        }
    }

    public LuyenDanResult Refine(CharacterData characterData, LuyenDanRequest request)
    {
        var main1 = request.ChuDuoc;
        var sub1 = request.PhuDuoc1;
        var sub2 = request.PhuDuoc2;
        var primer = request.DuocDan;
        var furnaceId = request.FurnaceId;
        var count = request.Count;
        var actualCount = count;
        var furnace = Furnaces.FirstOrDefault(x => x.SelfId == furnaceId);
        if (furnace == null)
        {
            throw new Exception(RefineMessage.FurnaceNotFound);
        }

        ItemRef? Get(LuyenDanRequest.Herbs herbs)
        {
            if (herbs.IsDefault()) return null;
            if (herbs.Count <= 0) throw new Exception(RefineMessage.QuantityInvalid);
            var myHerb = Herbs.FirstOrDefault(x => x.Id == herbs.Id);
            if (myHerb == null)
            {
                throw new Exception(RefineMessage.HerbsNotFound);
            }

            var myCount = myHerb.Count;
            if (myCount - herbs.Count * count < 0) throw new Exception(RefineMessage.NotEnoughQuantity);
            return myHerb;
        }

        (RecipeData? dataRes, RecipeData.Recipe? recipeRes) FindResRecipe()
        {
            var recipes = GameConfig.LuyenDanResources.Recipes;
            foreach (var recipeData in recipes)
            {
                foreach (var r in recipeData.Recipes)
                {
                    if (r.IsValid(main1, sub1, sub2, primer))
                    {
                        return (recipeData, r);
                    }
                }
            }

            return (null, null);
        }

        var chuDuoc = Get(main1);
        var phuDuoc1 = Get(sub1);
        var phuDuoc2 = Get(sub2);
        var duocDan = Get(primer);
        var modify = new InventoryModify();

        void Decrease(ItemRef? itemRef, LuyenDanRequest.Herbs herbs)
        {
            if (itemRef != null)
            {
                int itemCount = itemRef.Count;
                itemCount -= herbs.Count * count;
                if (itemCount <= 0)
                {
                    Herbs.Remove(itemRef);
                    Tracking.Items.Delete(itemRef);
                    modify.Items.Delete(itemRef);
                }
                else
                {
                    itemRef.Count = (ushort)itemCount;
                    Tracking.Items.Update(itemRef);
                    modify.Items.Update(itemRef);
                }
            }
        }

        Decrease(chuDuoc, main1);
        Decrease(phuDuoc1, sub1);
        Decrease(phuDuoc2, sub2);
        Decrease(duocDan, sub1);
        var (dataRes, recipeRes) = FindResRecipe();
        var baseTime = 3;
        var tuoiTac = characterData.Stats.TuoiTac;
        if (dataRes != null)
        {
            var myRecipe = Recipes.FirstOrDefault(x => x.Id == dataRes.Id);
            if (myRecipe == null)
            {
                actualCount = 0;
                for (int i = 0; i < count; i++)
                {
                    if (RandomUtils.RandomChance(100))
                    {
                        actualCount++;
                    }
                }
                var success = actualCount > 0;
                if (!success)
                {
                    return new LuyenDanResult()
                    {
                        Success = true,
                        Message = RefineMessage.ThereNoRecipeRefineFail,
                        ItemRefs = new(),
                        Modify = modify,
                        NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
                    };
                }
            }

            var linhDan = dataRes.LinhDan;
            var tier = linhDan.Tier;
            var furnaceData = GameConfig.LuyenDanResources.TempFurnaceDatas.First(x => x.Id == furnace.Id);
            var tierDiff = tier - furnaceData.Tier;
            var baseLossDurability = tierDiff > 0 ? tierDiff * 50 : 2;
            var loss = baseLossDurability * count;
            baseTime = (int)tier + 3;
            int dur = furnace.Durability;
            dur -= loss;
            if (dur <= 0)
            {
                Furnaces.Remove(furnace);
                furnaceIdAllocator.ReleaseId(furnace.SelfId);
                Tracking.Furnaces.Delete(furnace);
                modify.Furnaces.Delete(furnace);
                return new LuyenDanResult()
                {
                    Success = true,
                    Message = $"-{loss} độ bền, {furnaceData.Name} chỉ còn {dur} độ bền dẫn đến nổ lò",
                    ItemRefs = new(),
                    Modify = modify,
                    NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
                };
            }

            furnace.Durability = (sbyte)dur;
            Tracking.Furnaces.Update(furnace);
            modify.Furnaces.Update(furnace);
            var linhDanRef = new ItemRef()
            {
                Id = (ushort)linhDan.Id,
                Count = (ushort)actualCount,
                Type = ItemType.LinhDan
            };
            Add(LinhDans, linhDanRef);
            modify.Items.Add(linhDanRef);
            return new LuyenDanResult()
            {
                Success = true,
                Message = RefineMessage.Success,
                ItemRefs = new() { linhDanRef },
                Modify = modify,
                NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
            };
        }

        return new LuyenDanResult()
        {
            Success = true,
            Message = RefineMessage.RecipeInvalid,
            ItemRefs = new(),
            Modify = modify,
            NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
        };
    }

    public void Add(ItemRef itemRef)
    {
        var type = itemRef.Type;
        if (type == ItemType.Herb)
        {
            Add(Herbs, itemRef);
        }
        else if (type == ItemType.LinhDan)
        {
            Add(LinhDans, itemRef);
        }
        else if (type == ItemType.ThanThong)
        {
            Add(ThanThongs, itemRef);
        }
        else if (type == ItemType.CongPhap)
        {
            Add(CongPhaps, itemRef);
        }
        else if (type == ItemType.Recipe)
        {
            Add(Recipes, itemRef);
        }
        else if (type == ItemType.Material)
        {
            Add(Materials, itemRef);
        }
    }

    public void Add(ItemRef itemRef, int count)
    {
        itemRef.CharId = CharId;
        var array = new[] { Herbs, LinhDans, ThanThongs, CongPhaps, Recipes, Materials };
        foreach (var itemRefse in array)
        {
            if (itemRefse.TryGetValue(itemRef, out var item))
            {
                item.Count = (ushort)(itemRef.Count + count);
                if (item.Count == 0)
                {
                    itemRefse.Remove(item);
                    Tracking.Items.Delete(item);
                }
                else
                {
                    Tracking.Items.Update(item);
                }

                return;
            }
        }
    }

    public bool Update(ItemRef itemRef)
    {
        var array = new[] { Herbs, LinhDans, ThanThongs, CongPhaps, Recipes, Materials };
        return array.Any(x =>
        {
            if (x.TryGetValue(itemRef, out var item))
            {
                item.Count = itemRef.Count;
                if (item.Count == 0)
                {
                    x.Remove(item);
                    Tracking.Items.Delete(item);
                }
                else
                {
                    Tracking.Items.Update(item);
                }

                return true;
            }

            return false;
        });
    }

    public bool Remove(ItemRef itemRef)
    {
        itemRef.CharId = CharId;
        var array = new[] { Herbs, LinhDans, ThanThongs, CongPhaps, Recipes, Materials };
        return array.Any(x =>
        {
            if (x.Remove(itemRef))
            {
                Tracking.Items.Delete(itemRef);
                return true;
            }

            return false;
        });
    }

    private void Add(HashSet<ItemRef> inventory, ItemRef itemRef)
    {
        itemRef.CharId = CharId;
        if (inventory.TryGetValue(itemRef, out var item))
        {
            item.Count += itemRef.Count;
            Tracking.Items.Update(item);
        }
        else
        {
            inventory.Add(itemRef);
            Tracking.Items.Add(itemRef);
        }
    }

    private void Add(HashSet<Equipment> inventory, Equipment equipment)
    {
        equipment.CharId = CharId;
        equipment.SelfId = equipmentIdAllocator.GetId();
        inventory.Add(equipment);
        Tracking.Equipments.Add(equipment);
    }

    public void Add(Equipment equipment)
    {
        var type = equipment.GetEquipmentType();
        switch (type)
        {
            case EquipmentType.Weapon:
                Add(VuKhis, equipment);
                break;
            case EquipmentType.Armor:
                Add(PhongCus, equipment);
                break;
            case EquipmentType.Jewelry:
                Add(TrangSucs, equipment);
                break;
        }
    }

    public bool Update(Equipment equipment)
    {
        var array = new[] { VuKhis, PhongCus, TrangSucs };
        return array.Any(x =>
        {
            if (x.TryGetValue(equipment, out var e))
            {
                e.Name = equipment.Name;
                Tracking.Equipments.Update(e);
                return true;
            }

            return false;
        });
    }

    public bool Remove(Equipment equipment)
    {
        equipment.CharId = CharId;
        var array = new[] { VuKhis, PhongCus, TrangSucs };
        return array.Any(x =>
        {
            if (x.Remove(equipment))
            {
                Tracking.Equipments.Delete(equipment);
                return true;
            }

            return false;
        });
    }

    public bool RemoveEquipment(ushort seldId)
    {
        var equipment = new Equipment()
        {
            SelfId = seldId,
        };
        return Remove(equipment);
    }

    public void Add(NgoDao ngoDao)
    {
        ngoDao.CharId = CharId;
        NgoDaos.Add(ngoDao);
        Tracking.NgoDaos.Add(ngoDao);
    }

    public int AddOrUpdate(CamNgo camNgo)
    {
        if (CamNgos.TryGetValue(camNgo, out var cam))
        {
            return Update(cam, camNgo);
        }

        return Add(camNgo);
    }

    public int Update(CamNgo oldValue, CamNgo newValue)
    {
        var p = CharacterData.Progression;
        var newNgoDaoDiem = p.NgoDaoDiem;
        bool crossed = NgoDaoResource.CamNgoRequire.Any(x => oldValue.Value < x && newValue.Value >= x);
        if (crossed)
        {
            newNgoDaoDiem++;
            p.SetNgoDaoDiem(newNgoDaoDiem);
        }

        oldValue.Value = newValue.Value;
        Tracking.CamNgos.Update(oldValue);
        return newNgoDaoDiem;
    }

    public int Add(CamNgo camNgo)
    {
        var p = CharacterData.Progression;
        var newNgoDaoDiem = p.NgoDaoDiem + 1;
        p.SetNgoDaoDiem(newNgoDaoDiem);
        camNgo.CharId = CharId;
        CamNgos.Add(camNgo);
        Tracking.CamNgos.Add(camNgo);
        return newNgoDaoDiem;
    }

    public void AddOrUpdate(CongPhap congPhap)
    {
        if (CongPhapOwners.TryGetValue(congPhap, out var congPhapOwner))
        {
            Update(congPhapOwner, congPhap);
        }
        else
        {
            Add(congPhap);
        }
    }

    public void Update(CongPhap oldValue, CongPhap newValue)
    {
        oldValue.Level = newValue.Level;
        Tracking.CongPhaps.Update(oldValue);
    }

    public void Add(CongPhap congPhap)
    {
        congPhap.CharId = CharId;
        CongPhapOwners.Add(congPhap);
        Tracking.CongPhaps.Add(congPhap);
    }

    public void Remove(CongPhap congPhap)
    {
        congPhap.CharId = CharId;
        CongPhapOwners.Remove(congPhap);
        Tracking.CongPhaps.Delete(congPhap);
    }

    public void Add(ThanThong thanThong)
    {
        thanThong.CharId = CharId;
        ThanThongOwners.Add(thanThong);
        Tracking.ThanThongs.Add(thanThong);
    }

    public void ApplyModify(InventoryModify m)
    {
        m.Apply(this, Tracking);
    }

    public SetEquipmentNameResult SetEquipmentName(SetEquipmentName request)
    {
        var name = request.Name;
        if (name.Length > 30)
        {
            return new SetEquipmentNameResult()
            {
                Id = request.Id,
                SelfId = request.SelfId,
                Success = false,
                Message = LuyenKhiMessage.NameTooLong,
                Name = name,
            };
        }

        var e = new Equipment()
        {
            Id = request.Id,
            SelfId = request.SelfId,
            Name = name,
        };
        if (!Update(e))
        {
            return new SetEquipmentNameResult()
            {
                Id = request.Id,
                SelfId = request.SelfId,
                Success = false,
                Message = LuyenKhiMessage.EquipmentNotFound,
                Name = name,
            };
        }

        return new SetEquipmentNameResult()
        {
            Id = request.Id,
            SelfId = request.SelfId,
            Success = true,
            Message = LuyenKhiMessage.SetNameSucess,
            Name = name,
        };
    }

    public LuyenKhiResult LuyenKhi(LuyenKhiRequest request)
    {
        var ids = request.MaterialIds;
        var res = LuyenKhiResources.Instance;
        var materials = ids.Select(x =>
        {
            var material = res.Materials.FirstOrDefault(y => y.Id == x);
            if (material == null)
            {
                throw new Exception(LuyenKhiMessage.MaterialNotFound);
            }

            return material;
        }).ToArray();
        var g = materials.GroupBy(x => x.Id)
            .Select(x => new
            {
                Material = x.First(),
                Count = (ushort)x.Count()
            })
            .ToArray();
        var itemRefs = g.Select(x =>
        {
            var itemRef = new ItemRef()
            {
                Id = (ushort)x.Material.Id,
                Type = ItemType.Material,
                Count = x.Count
            };
            if (Materials.TryGetValue(itemRef, out var material))
            {
                var newCount = material.Count - itemRef.Count;
                if (newCount < 0)
                {
                    throw new Exception(LuyenKhiMessage.NotEnoughMaterials);
                }

                return new
                {
                    Material = material,
                    x.Count
                };
            }

            throw new Exception(LuyenKhiMessage.DoesNotOwnMaterial);
        });
        var modify = new InventoryModify();
        var matConfigs = res.MaterialGradeConfigs;
        var grade = 0;
        foreach (var material in materials)
        {
            var matConfig = matConfigs.First(x => x.Tier == material.Tier);
            grade += matConfig.Grade;
        }

        var equipmentGradeConfig = res.EquipmentGrades.Last(x => x.MinGrade <= grade);
        var tier = equipmentGradeConfig.Tier;
        var selectedType = request.SelectedType;
        var lianQiEffects = res.LianQiEffects;
        var sumGroup = materials
            .GroupBy(x => new
            {
                x.CultivationElement,
                x.YinYangType,
            })
            .Select(k => new
            {
                Materials = k.ToList(),
                Effect = lianQiEffects.First(i =>
                        i.CultivationElement == k.Key.CultivationElement && i.YinYangType == k.Key.YinYangType)
                    .Effects.First(i => i.ArtifactType == selectedType),
            })
            .GroupBy(x => x.Effect.EffectType)
            .Select(group => new
            {
                Sum = group.SelectMany(x => x.Materials).Sum(x => matConfigs.First(i => i.Tier == x.Tier).Grade),
                group.First().Effect,
            })
            .ToArray();
        var effects = sumGroup
            .Select(x =>
            {
                var sum = x.Sum;
                var effect = x.Effect;
                var max = (int)Math.Round(1.0f * sum / effect.Value, MidpointRounding.AwayFromZero);
                var min = (int)Math.Round(0.5f * max, MidpointRounding.AwayFromZero);
                var value = (byte)new Random().Next(min, max + 1);
                return new Effect()
                {
                    Type = effect.EffectType,
                    Value = value,
                };
            })
            .Where(x => x.Value != 0)
            .ToArray();
        var random = new Random();
        var item = res.RewardItems
            .Where(x => x.Tier == tier & x.Type == selectedType)
            .OrderBy(x => random.Next())
            .First();

        var equipmentTier = res.EquipmentTiers.First(x => x.Tier == tier);
        var dayCost = equipmentTier.DayCost;
        var stats = CharacterData.Stats;
        var newTuoiTac = stats.TuoiTac.Add(TimeSpan.FromDays(dayCost));
        stats.SetTuoiTac(newTuoiTac);
        var equipment = new Equipment()
        {
            Id = (ushort)item.Id,
            Effects = effects,
            Name = CharacterData.Name,
            MaterialIds = ids
        };
        foreach (var i in itemRefs)
        {
            var itemRef = i.Material;
            var originalCount = itemRef.Count;
            var newCount = originalCount - i.Count;
            if (newCount <= 0)
            {
                Remove(itemRef);
                modify.Items.Delete(itemRef);
            }
            else
            {
                itemRef.Count = (ushort)newCount;
                Update(itemRef);
                modify.Items.Update(itemRef);
            }
        }

        Add(equipment);
        modify.Equipments.Add(equipment);
        return new LuyenKhiResult()
        {
            Success = true,
            Message = LuyenKhiMessage.Success,
            Item = equipment,
            NewTuoiTac = newTuoiTac,
            Modify = modify
        };
    }

    public bool EquipThanThong(ushort id)
    {
        var thanThong = new ThanThong()
        {
            Id = id,
            CharId = CharId
        };
        var add = ThanThongOwners.Add(thanThong);
        if (add)
        {
            Tracking.ThanThongs.Add(thanThong);
        }

        return add;
    }

    public int UpdateNgoDaoWithSkill(BaseSkill skill, int baseAdd, ref List<CamNgo> camNgoAdd, int count = 1)
    {
        ND.ETypeSkill ConvertETypeSkillToETypeND(ETypeSkill eTypeSkill)
        {
            return (ND.ETypeSkill)(int)eTypeSkill;
        }

        var newNgoDaoDiem = CharacterData.Progression.NgoDaoDiem;
        foreach (var typeSkill in skill.eTypeSkill)
        {
            var camNgoType = ConvertETypeSkillToETypeND(typeSkill);
            var userCamNgo = CamNgos.FirstOrDefault(x => x.Id == camNgoType);
            int currentUserCamNgoValue = userCamNgo != null ? userCamNgo.Value : 0;

            var camNgo = new CamNgo
            {
                Id = camNgoType,
                Value = (baseAdd * count * (int)skill.eTierSkill) + currentUserCamNgoValue,
            };
            newNgoDaoDiem = AddOrUpdate(camNgo);
            camNgoAdd.Add(camNgo);
        }

        return newNgoDaoDiem;
    }
}

public static class LuyenKhiMessage
{
    public const string MaterialSelectionRangeMessage = "Bạn cần chọn ít nhất 1 và tối đa 10 nguyên liệu";
    public const string MaterialNotFound = "Nguyên liệu không tồn tại";
    public const string NotEnoughMaterials = "Bạn chưa sở hữu đủ số lượng nguyên liệu";
    public const string DoesNotOwnMaterial = "Bạn chưa sở hữu nguyên liệu";
    public const string Success = "Luyện thành công";
    public const string NameTooLong = "Tên trang bị không được quá 30 ký tự";
    public const string SetNameSucess = "Đặt tên thành công";
    public const string EquipmentNotFound = "Trang bị không tồn tại";
}

public static class RefineMessage
{
    public const string FurnaceNotFound = "Không tìm thấy đan lu";
    public const string QuantityInvalid = "Số lượng thảo dược không hợp lệ";
    public const string NotEnoughQuantity = "Không đủ số lượng số lượng thảo dược";
    public const string HerbsNotFound = "Thảo dược không tồn tại";
    public const string ThereNoRecipeRefineFail = "Bạn chưa sở hữu công thức, luyện đan thất bại";
    public const string RecipeInvalid = "Công thức không đúng, luyện đan thất bại";
    public const string Success = "Luyện đan thành công";
}