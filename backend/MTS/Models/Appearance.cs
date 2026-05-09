using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class Appearance : IMemorySerializable<Appearance>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }
    public Dictionary<string, string> Skins { get; set; } = new();
    public Dictionary<string, Color> Colors { get; set; } = new();
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharacterData { get; set; }
    public void Deserialize(IReader reader, ref Appearance value)
    {
        value.Skins = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray());
        value.Colors = DictionaryUtils.CreateDictionary(reader.ReadArray(), reader.ReadArray<Color>());
    }

    public int ByteSize()
    {
        return Skins.Keys.ByteSize() + Skins.Values.ByteSize() + Colors.Keys.ByteSize() + Colors.Values.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.WriteArray(Skins.Keys);
        writer.WriteArray(Skins.Values);
        writer.WriteArray(Colors.Keys);
        writer.WriteArray(Colors.Values);
    }

    public void CopyFrom(Appearance other)
    {
        CharId = other.CharId;
        Skins = new (other.Skins);
        Colors = new (other.Colors);
        CharacterData = other.CharacterData;
    }
}