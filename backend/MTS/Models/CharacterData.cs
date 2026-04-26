using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class CharacterData : IMemorySerializable<CharacterData>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int UserId { get; set; }
    [Key] public int Id { get; set; }
    public Gender Gender { get; set; }
    public string Name { get; set; }

    public EMonPhai MonPhai { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public CharacterStats Stats { get; set; } = new();
    public Progression Progression { get; set; } = new();
    public Appearance Appearance { get; set; } = new();
    public Equipment Equipment { get; set; } = new();
    public Social Social { get; set; } = new();
    public InventoryManager InventoryManager { get; set; } = new();

    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public UserData User { get; set; }


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
        Equipment.CopyFrom(clone.Equipment);
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
               + Equipment.ByteSize()
               + MonPhai.ByteSize() 
               + InventoryManager.ByteSize()
               + CreatedAt.ByteSize()
               + Social.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Gender);
        writer.Write(Name);
        writer.Write(Stats);
        writer.Write(Progression);
        writer.Write(Appearance);
        writer.Write(Equipment);
        writer.Write(MonPhai);
        writer.Write(InventoryManager);
        writer.Write(CreatedAt);
        writer.Write(Social);
    }

    public void Deserialize(IReader reader, ref CharacterData value)
    {
        value.Id = reader.Read<int>();
        value.Gender = reader.Read<Gender>();
        value.Name = reader.Read();
        value.Stats = reader.Read<CharacterStats>();
        value.Progression = reader.Read<Progression>();
        value.Appearance = reader.Read<Appearance>();
        value.Equipment = reader.Read<Equipment>();
        value.MonPhai = reader.Read<EMonPhai>();
        value.InventoryManager = reader.Read<InventoryManager>();
        value.CreatedAt = reader.Read<DateTime>();
        value.Social = reader.Read<Social>();
    }
    public bool IsInventoryDefault()
    {
        return InventoryManager.IsDefault();
    }
    public async Task SaveChanges(AppDbContext db)
    {
        var charData = await db.Characters
            .FirstAsync(x => x.Id == Id);
        charData.Clone0(this);
        if (!Stats.IsDefault())
        {
            await Stats.SaveChanges(db);
        }
        if (!Progression.IsDefault())
        {
            await Progression.SaveChanges(db);
        }

        if (!Equipment.IsDefault())
        {
            await Equipment.SaveChanges(db);
        }

        if (!Social.IsDefault())
        {
            await Social.SaveChanges(db);
        }
        if (!InventoryManager.IsDefault())
        {
            await InventoryManager.SaveChanges(db);
        }
    }
}