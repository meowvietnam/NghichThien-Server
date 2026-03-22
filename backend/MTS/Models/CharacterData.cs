using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MTS.Game;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class CharacterData : IMemorySerializable<CharacterData>
{
    [Key]
    public int Id { get; set; }
    public Gender Gender { get; set; }
    public string Name { get; set; }
    public int Level { get; set; } = 1;
    public int CurrentExp { get; set; }
    public CharacterStats Stats { get; set; } = new CharacterStats();
    public Dictionary<string, string> Skins { get; set; }
    public Dictionary<string, Color> Colors { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public UserData User { get; set; }
    public CharacterData()
    {
    }

    public CharacterData(CharacterData clone)
    {
        Id = clone.Id;
        Gender = clone.Gender;
        Name = clone.Name;
        Level = clone.Level;
        CurrentExp = clone.CurrentExp;
        Stats.CopyFrom(clone.Stats);
        Skins = new(clone.Skins);
        Colors = new(clone.Colors);
    }

    public int ByteSize()
    {
        return Id.ByteSize()
               + Gender.ByteSize() + Name.ByteSize()
               + Level.ByteSize()
               + CurrentExp.ByteSize()
               + Stats.ByteSize()
               + Skins.Keys.ByteSize() + Skins.Values.ByteSize()
               + Colors.Keys.ByteSize() + Colors.Values.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Gender);
        writer.Write(Name);
        writer.Write(Level);
        writer.Write(CurrentExp);
        writer.Write(Stats);
        writer.WriteArray(Skins.Keys);
        writer.WriteArray(Skins.Values);
        writer.WriteArray(Colors.Keys);
        writer.WriteArray(Colors.Values);
    }

    public void Deserialize(IReader reader, ref CharacterData value)
    {
        value.Id = reader.Read<int>();
        value.Gender = reader.Read<Gender>();
        value.Name = reader.Read();
        value.Level = reader.Read<int>();
        value.CurrentExp = reader.Read<int>();
        value.Stats = reader.Read<CharacterStats>();
        value.Skins = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray());
        value.Colors = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray<Color>());
    }
    public bool IsSkinEmpty()
    {
        return Skins.Count == 0 || Colors.Count == 0;
    }
}