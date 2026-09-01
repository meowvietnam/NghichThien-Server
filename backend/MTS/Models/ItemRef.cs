using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Converter;
using MTS.Interfaces;
using MTS.Utils;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

[JsonConverter(typeof(ItemRefConverter))]
[MemoryPackable]
public partial class ItemRef : IEquatable<ItemRef>, ICustomEquals<ItemRef>, IDeepClone<ItemRef>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    public ushort Id { get; set; }
    public ItemType Type { get; set; }
    public int Count { get; set; }
    [MemoryPackConstructor]
    public ItemRef()
    {
    }

    public ItemRef(ushort id, ItemType type, int count)
    {
        Id = id;
        Type = type;
        Count = count;
    }

    public bool CustomEquals(ItemRef other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Type == other.Type && Count == other.Count;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Type);
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

    public override string ToString()
    {
        return $"{Id} {Type} {Count}";
    }
}