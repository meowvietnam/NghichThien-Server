using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

public class InventoryManager : IMemorySerializable<InventoryManager>
{
    public HashSet<FurnaceData> Furnaces { get; set; } = new();
    public HashSet<int> Recipes { get; set; } = new();
    public HashSet<ItemRef> Herbs { get; set; } = new();
    [NotMapped]
    private int furnaceId = 0;
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }
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
        return Furnaces.ByteSize() + Recipes.ByteSize() + Herbs.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.WriteArray(Furnaces);
        writer.WriteArray(Recipes);
        writer.WriteArray(Herbs);
    }
    public void Deserialize(IReader reader, ref InventoryManager value)
    {
        value.Furnaces = reader.ReadArray<FurnaceData>().ToHashSet();
        value.Recipes = reader.ReadArray<int>().ToHashSet();
        value.Herbs =reader.ReadArray<ItemRef>().ToHashSet();
    }
    public void Clone(InventoryManager other)
    {
        Furnaces = new HashSet<FurnaceData>(other.Furnaces);
        Recipes = new HashSet<int>(other.Recipes);
        Herbs = new HashSet<ItemRef>(other.Herbs);
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
}