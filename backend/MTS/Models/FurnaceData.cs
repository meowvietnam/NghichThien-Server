using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Converter;
using MTS.Interfaces;
using MTS.Utils;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;
[JsonConverter(typeof(FurnaceDataConverter))]
[MemoryPackable]
public partial class FurnaceData :  IEquatable<FurnaceData> ,ICustomEquals<FurnaceData>, IDeepClone<FurnaceData>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public ushort SelfId { get; set; }
    public ushort Id { get; set; }
    public sbyte Durability { get; set; }
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public InventoryManager Inventory { get; set; }

    public FurnaceData DeepClone()
    {
        return new FurnaceData()
        {
            SelfId = SelfId,
            Id = Id,
            Durability = Durability,
        };
    }

    public override int GetHashCode()
    {
        return SelfId;
    }
    public bool Equals(FurnaceData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SelfId == other.SelfId;
    }

    public bool CustomEquals(FurnaceData other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SelfId == other.SelfId  && Id == other.Id && Durability == other.Durability;
    }
}