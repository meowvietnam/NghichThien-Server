using MTS.Converter;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;
[JsonConverter(typeof(ItemRefConverter))]
public class ItemRef : IMemorySerializable<ItemRef>
{
    public int Id { get; set; }
    public ItemType Type { get; set; }
    public int Count { get; set; }
    public ItemRef()
    {
    }
    public ItemRef(int id, ItemType type, int count)
    {
        Id = id;
        Type = type;
        Count = count;
    }

    public bool IsFurnace()
    {
        return Type == ItemType.Furnace;
    }
    public int ByteSize()
    {
        return Id.ByteSize() + Type.ByteSize() + Count.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Type);
        writer.Write(Count);
    }
    public void Deserialize(IReader reader, ref ItemRef value)
    {
        value.Id = reader.Read<int>();
        value.Type = reader.Read<ItemType>();
        value.Count = reader.Read<int>();
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Type);
    }
}