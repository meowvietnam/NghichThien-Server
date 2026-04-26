using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.LuyenDan;
using MTS.Interfaces;
using MTS.Network.Models;
using MTS.Utils;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

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
    [NotMapped] public HashSet<ItemRef> VuKhis { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> PhongCus { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> TrangSucs { get; set; } = new();
    [NotMapped] public HashSet<ItemRef> LinhDans { get; set; } = new();
    public HashSet<NgoDao> NgoDaos { get; set; } = new();
    public HashSet<CamNgo> CamNgos { get; set; } = new();
    public HashSet<CongPhap> CongPhapOwners { get; set; } = new();
    public HashSet<ThanThong> ThanThongOwners { get; set; } = new();
    
    [NotMapped] private IdAllocator<ushort> furnaceIdAllocator;
    [NotMapped] private InventoryTracking tracking;
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public CharacterData CharacterData { get; set; }

    public void Init()
    {
        tracking = new InventoryTracking();
        furnaceIdAllocator = new IdAllocator<ushort>(0, x => (ushort)(x + 1), x => (ushort)(x - 1));
        var usedIds = Furnaces.Select(x => x.SelfId).ToArray();
        foreach (var usedId in usedIds)
        {
            furnaceIdAllocator.AddUsedId(usedId);
        }
    }

    public async Task Load(AppDbContext db)
    {
        await using var db2 = await Program.CreateDbAsync();
        var itemsTask = db.Items
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .GroupBy(x => x.Type)
            .ToDictionaryAsync(x => x.Key, x => x.ToHashSet());
        var furnacesTask = db2.Furnaces
            .AsNoTracking()
            .Where(x => x.CharId == CharId)
            .ToHashSetAsync();
        await Task.WhenAll(itemsTask, furnacesTask);
        Furnaces = furnacesTask.Result;
        var items = itemsTask.Result;
        Recipes = items.GetValueOrDefault(ItemType.Recipe, new());
        Herbs = items.GetValueOrDefault(ItemType.Herb, new());
        ThanThongs = items.GetValueOrDefault(ItemType.ThanThong, new());
        CongPhaps = items.GetValueOrDefault(ItemType.CongPhap, new());
        VuKhis = items.GetValueOrDefault(ItemType.VuKhi, new());
        PhongCus = items.GetValueOrDefault(ItemType.PhongCu, new());
        TrangSucs = items.GetValueOrDefault(ItemType.TrangSuc, new());
        LinhDans = items.GetValueOrDefault(ItemType.LinhDan, new());
        Init();
    }
    public void AddFurnace(FurnaceData furnace)
    {
        furnace.CharId = CharId;
        var furnaceId = furnaceIdAllocator.GetId();
        furnace.SelfId = furnaceId;
        Furnaces.Add(furnace);
        tracking.Furnaces.Add(furnace);
    }

    public int ByteSize()
    {
        return Furnaces.ByteSize() + Recipes.ByteSize() + Herbs.ByteSize() + ThanThongs.ByteSize() +
               CongPhaps.ByteSize() + VuKhis.ByteSize() + PhongCus.ByteSize() + TrangSucs.ByteSize() +
               LinhDans.ByteSize() + NgoDaos.ByteSize() +  CamNgos.ByteSize() +  CongPhapOwners.ByteSize()
               + ThanThongOwners.ByteSize();
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
    }

    public void Deserialize(IReader reader, ref InventoryManager value)
    {
        value.Furnaces = reader.ReadArray<FurnaceData>().ToHashSet();
        value.Recipes = reader.ReadArray<ItemRef>().ToHashSet();
        value.Herbs = reader.ReadArray<ItemRef>().ToHashSet();
        value.ThanThongs = reader.ReadArray<ItemRef>().ToHashSet();
        value.CongPhaps = reader.ReadArray<ItemRef>().ToHashSet();
        value.VuKhis = reader.ReadArray<ItemRef>().ToHashSet();
        value.PhongCus = reader.ReadArray<ItemRef>().ToHashSet();
        value.TrangSucs = reader.ReadArray<ItemRef>().ToHashSet();
        value.LinhDans = reader.ReadArray<ItemRef>().ToHashSet();
        value.NgoDaos = reader.ReadArray<NgoDao>().ToHashSet();
        value.CamNgos = reader.ReadArray<CamNgo>().ToHashSet();
        value.CongPhapOwners = reader.ReadArray<CongPhap>().ToHashSet();
        value.ThanThongOwners = reader.ReadArray<ThanThong>().ToHashSet();
    }


    public void Clone(InventoryManager other)
    {
        CharId = other.CharId;
        Furnaces = new HashSet<FurnaceData>(other.Furnaces);
        Recipes = new HashSet<ItemRef>(other.Recipes);
        Herbs = new HashSet<ItemRef>(other.Herbs);
        ThanThongs = new HashSet<ItemRef>(other.ThanThongs);
        CongPhaps = new HashSet<ItemRef>(other.CongPhaps);
        VuKhis = new HashSet<ItemRef>(other.VuKhis);
        PhongCus = new HashSet<ItemRef>(other.PhongCus);
        TrangSucs = new HashSet<ItemRef>(other.TrangSucs);
        LinhDans = new HashSet<ItemRef>(other.LinhDans);
        NgoDaos = new HashSet<NgoDao>(other.NgoDaos);
        CamNgos = new HashSet<CamNgo>(other.CamNgos);
        CongPhapOwners = new HashSet<CongPhap>(other.CongPhapOwners);
        ThanThongOwners = new HashSet<ThanThong>(other.ThanThongOwners);
        furnaceIdAllocator = other.furnaceIdAllocator;
        tracking = other.tracking;
        CharacterData = other.CharacterData;
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public Task SaveChanges(AppDbContext db)
    {
        lock (tracking)
        {
            tracking.Apply(db);
            tracking.Clear();
        }
        return Task.CompletedTask;
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
        else if (type == ItemType.VuKhi)
        {
            Add(VuKhis, itemRef);
        }
        else if (type == ItemType.PhongCu)
        {
            Add(PhongCus, itemRef);
        }
        else if (type == ItemType.TrangSuc)
        {
            Add(TrangSucs, itemRef);
        }
        else if (type == ItemType.Recipe)
        {
            Add(Recipes, itemRef);
        }
    }

    private void Add(HashSet<ItemRef> inventory, ItemRef itemRef)
    {
        itemRef.CharId = CharId;
        if (inventory.TryGetValue(itemRef, out var item))
        {
            item.Count += itemRef.Count;
            tracking.Items.Update(item);
        }
        else
        {
            inventory.Add(itemRef);
            tracking.Items.Add(itemRef);
        }
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
                AddFurnace(furnace);
                result.Furnaces.Add(furnace);
            }
            else
            {
                result.OtherItems.Add(itemRef);
                Add(itemRef);
            }
        }
        return result;
    }
    public LuyenDanResult Refine(CharacterData characterData, LuyenDanRequest request)
    {
        var main1 = request.ChuDuoc;
        var sub1 = request.PhuDuoc1;
        var sub2 = request.PhuDuoc2;
        var primer = request.DuocDan;
        var furnaceId = request.FurnaceId;
        var count = request.Count;
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

        void Decrease(ItemRef? itemRef, LuyenDanRequest.Herbs herbs)
        {
            if (itemRef != null)
            {
                int itemCount = itemRef.Count;
                itemCount -= herbs.Count * count;
                if (itemCount <= 0)
                {
                    Herbs.Remove(itemRef);
                    tracking.Items.Delete(itemRef);
                }
                else
                {
                    itemRef.Count = (ushort)itemCount;
                    tracking.Items.Update(itemRef);
                }
            }
        }
        var chuDuoc = Get(main1);
        var phuDuoc1 = Get(sub1);
        var phuDuoc2 = Get(sub2);
        var duocDan = Get(primer);
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
                var success = new Random().NextSingle() >= 0.5f;
                if (!success)
                {
                    return new LuyenDanResult()
                    {
                        Success = true,
                        Message = RefineMessage.ThereNoRecipeRefineFail,
                        ItemRefs = new(),
                        Inventory = this,
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
                tracking.Furnaces.Delete(furnace);
                return new LuyenDanResult()
                {
                    Success = true,
                    Message = $"-{loss} độ bền, {furnaceData.Name} chỉ còn {dur} độ bền dẫn đến nổ lò",
                    ItemRefs = new(),
                    Inventory = this,
                    NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
                };
            }
            furnace.Durability = (sbyte)dur;
            tracking.Furnaces.Update(furnace);
            var linhDanRef = new ItemRef()
            {
                Id = (ushort)linhDan.Id,
                Count = (ushort)count,
                Type = ItemType.LinhDan
            };
            Add(LinhDans, linhDanRef);
            return new LuyenDanResult()
            {
                Success = true,
                Message = RefineMessage.Success,
                ItemRefs = new() { linhDanRef },
                Inventory = this,
                NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
            };
        }

        return new LuyenDanResult()
        {
            Success = true,
            Message = RefineMessage.RecipeInvalid,
            ItemRefs = new(),
            Inventory = this,
            NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
        };
    }
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