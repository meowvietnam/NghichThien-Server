using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.LuyenDan;
using MTS.Interfaces;
using MTS.Network.Models;
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
    public HashSet<ItemRef> Recipes { get; set; } = new();
    public HashSet<ItemRef> Herbs { get; set; } = new();
    public HashSet<ItemRef> ThanThongs { get; set; } = new();
    public HashSet<ItemRef> CongPhaps { get; set; } = new();
    public HashSet<ItemRef> VuKhis { get; set; } = new();
    public HashSet<ItemRef> PhongCus { get; set; } = new();
    public HashSet<ItemRef> TrangSucs { get; set; } = new();
    public HashSet<ItemRef> LinhDans { get; set; } = new();
    [NotMapped] private int furnaceId = 0;

    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public CharacterData CharacterData { get; set; }

    public void Init()
    {
        furnaceId = Furnaces.Any() ? Furnaces.MaxBy(x => x.SelfId).SelfId : 0;
    }

    public void AddFurnace(FurnaceData furnace)
    {
        Interlocked.Increment(ref furnaceId);
        furnace.SelfId = furnaceId;
        Furnaces.Add(furnace);
    }

    public int ByteSize()
    {
        return Furnaces.ByteSize() + Recipes.ByteSize() + Herbs.ByteSize() + ThanThongs.ByteSize() +
               CongPhaps.ByteSize() + VuKhis.ByteSize() + PhongCus.ByteSize() + TrangSucs.ByteSize() +
               LinhDans.ByteSize();
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
    }

    public void Clone(InventoryManager other)
    {
        Furnaces = new HashSet<FurnaceData>(other.Furnaces);
        Recipes = new HashSet<ItemRef>(other.Recipes);
        Herbs = new HashSet<ItemRef>(other.Herbs);
        ThanThongs = new HashSet<ItemRef>(other.ThanThongs);
        CongPhaps = new HashSet<ItemRef>(other.CongPhaps);
        VuKhis = new HashSet<ItemRef>(other.VuKhis);
        PhongCus = new HashSet<ItemRef>(other.PhongCus);
        TrangSucs = new HashSet<ItemRef>(other.TrangSucs);
        LinhDans = new HashSet<ItemRef>(other.LinhDans);
    }

    public InventoryManager DeepClone()
    {
        var other = new InventoryManager();
        other.Furnaces = DeepClone(Furnaces);
        other.Recipes = DeepClone(Recipes);
        other.Herbs = DeepClone(Herbs);
        other.ThanThongs = DeepClone(ThanThongs);
        other.CongPhaps = DeepClone(CongPhaps);
        other.VuKhis = DeepClone(VuKhis);
        other.PhongCus = DeepClone(PhongCus);
        other.TrangSucs = DeepClone(TrangSucs);
        other.LinhDans = DeepClone(LinhDans);
        return other;
    }

    private HashSet<T> DeepClone<T>(IEnumerable<T> collection) where T : IDeepClone<T>
    {
        return new HashSet<T>(collection.Select(x => x.DeepClone()));
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public async Task SaveChanges(AppDbContext db)
    {
        var inventory = await db.InventoryManagers.FirstAsync(x => x.CharId == CharId);
        inventory.Clone(this);
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
        if (inventory.TryGetValue(itemRef, out var item))
        {
            item.Count += itemRef.Count;
        }
        else
        {
            inventory.Add(itemRef);
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
                itemRef.Count = 1;
                var furnace = new FurnaceData()
                {
                    Ref = itemRef,
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

    public LuyenDanResult Refine(CharacterData characterData,LuyenDanRequest request)
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
                itemRef.Count -= herbs.Count * count;
                if (itemRef.Count <= 0)
                {
                    Herbs.Remove(itemRef);
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
            var furnaceData = GameConfig.LuyenDanResources.TempFurnaceDatas.First(x => x.Id == furnace.Ref.Id);
            var tierDiff = tier - furnaceData.Tier;
            var baseLossDurability = tierDiff > 0 ? tierDiff * 50 : 2;
            var loss = baseLossDurability * count;
            baseTime = (int)tier + 3;
            furnace.Durability -= loss;
            if (furnace.Durability <= 0)
            {
                Furnaces.Remove(furnace);
                return new LuyenDanResult()
                {
                    Success = true,
                    Message = $"-{loss} độ bền, {furnaceData.Name} chỉ còn {furnace.Durability} độ bền dẫn đến nổ lò",
                    ItemRefs = new(),
                    Inventory = this,
                    NewTuoiTac = tuoiTac.Add(TimeSpan.FromDays(baseTime * count)),
                };
            }

            var linhDanRef = new ItemRef()
            {
                Id = linhDan.Id,
                Count = count,
                Type = ItemType.LinhDan
            };
            LinhDans.Add(linhDanRef);
            return new LuyenDanResult()
            {
                Success = true,
                Message = RefineMessage.Success,
                ItemRefs = new() {linhDanRef},
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