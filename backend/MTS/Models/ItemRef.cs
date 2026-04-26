using MTS.Converter;
using MTS.Interfaces;
using MTS.Utils;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;
[JsonConverter(typeof(ItemRefConverter))]
public class ItemRef : IMemorySerializable<ItemRef>, IEquatable<ItemRef>, ICustomEquals<ItemRef>, IDeepClone<ItemRef>
{
    public ushort Id { get; set; }
    public ItemType Type { get; set; }
    public ushort Count { get; set; }
    public ItemRef()
    {
    }
    public ItemRef(ushort id, ItemType type, int count)
    {
        Id = id;
        Type = type;
        Count = (ushort)count;
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
        value.Id = reader.Read<ushort>();
        value.Type = reader.Read<ItemType>();
        value.Count = reader.Read<ushort>();
    }

    public bool CustomEquals(ItemRef other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Type == other.Type  && Count == other.Count;
    }

    
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, (int)Type);
    }
    public bool Equals(ItemRef? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Type == other.Type;
    }

    public ItemRef DeepClone()
    {
        return new ItemRef(Id, Type, Count);
    }
}