using MTS.Converter;
using MTS.Interfaces;
using MTS.Utils;
using Network;
using Network.Extensions;

namespace MTS.Models;
[Newtonsoft.Json.JsonConverter(typeof(FurnaceDataConverter))]
public class FurnaceData : IMemorySerializable<FurnaceData>, IEquatable<FurnaceData> ,ICustomEquals<FurnaceData>, IDeepClone<FurnaceData>
{
    public ushort SelfId { get; set; }
    public ushort Id { get; set; }
    public sbyte Durability { get; set; }
    public int ByteSize()
    {
        return SelfId.ByteSize() + Id.ByteSize()  + Durability.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(SelfId);
        writer.Write(Id);
        writer.Write(Durability);
    }
    public void Deserialize(IReader reader, ref FurnaceData value)
    {
        value.SelfId = reader.Read<ushort>();
        value.Id = reader.Read<ushort>();
        value.Durability = reader.Read<sbyte>();
    }

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
        return HashCode.Combine(SelfId, Id);
    }
    public bool Equals(FurnaceData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SelfId == other.SelfId && Id == other.Id;
    }

    public bool CustomEquals(FurnaceData other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SelfId == other.SelfId  && Id == other.Id && Durability == other.Durability;
    }
}