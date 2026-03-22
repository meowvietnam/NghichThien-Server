using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Game;

public class CreateSkinRequest : IMemorySerializable<CreateSkinRequest>
{
    public Gender Gender { get; set; }
    public Dictionary<string, string> Skins { get; set; } = new();
    public Dictionary<string, Color> Colors { get; set; } = new();

    public void Deserialize(IReader reader, ref CreateSkinRequest value)
    {
        value.Gender = reader.Read<Gender>();
        value.Skins = DictionaryUtils.CreateDictionary(reader.ReadArray(),reader.ReadArray());
        value.Colors = DictionaryUtils.CreateDictionary(reader.ReadArray(),reader.ReadArray<Color>());
    }

    public int ByteSize()
    {
        return Gender.ByteSize() + Skins.Keys.ByteSize() + Skins.Values.ByteSize() + Colors.Keys.ByteSize() + Colors.Values.ByteSize();
    }
    
    public void Serialize(IWriter writer)
    {
        writer.Write(Gender);
        writer.WriteArray(Skins.Keys);
        writer.WriteArray(Skins.Values);
        writer.WriteArray(Colors.Keys);
        writer.WriteArray(Colors.Values);
    }
    public void RoundColor(int digits)
    {
        foreach (var kv in Colors)
        {
            Colors[kv.Key] = Color.Round(kv.Value, digits);
        }
    }
}