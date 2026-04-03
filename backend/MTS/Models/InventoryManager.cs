using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
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
        if (!inventory.Add(itemRef))
        {
            var item = inventory.First(x => x.Id == itemRef.Id);
            item.Count += itemRef.Count;
        }
    }
}