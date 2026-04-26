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
    public ItemRef Ref { get; set; }
    public sbyte Durability { get; set; }
    public int ByteSize()
    {
        return SelfId.ByteSize() + Ref.ByteSize()  + Durability.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(SelfId);
        writer.Write(Ref);
        writer.Write(Durability);
    }
    public void Deserialize(IReader reader, ref FurnaceData value)
    {
        value.SelfId = reader.Read<ushort>();
        value.Ref = reader.Read<ItemRef>();
        value.Durability = reader.Read<sbyte>();
    }

    public FurnaceData DeepClone()
    {
        return new FurnaceData()
        {
            SelfId = SelfId,
            Durability = Durability,
            Ref = Ref.DeepClone(),
        };
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SelfId, Ref);
    }
    public bool Equals(FurnaceData? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SelfId == other.SelfId && Ref.Equals(other.Ref);
    }

    public bool CustomEquals(FurnaceData other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SelfId == other.SelfId && Ref.Equals(other.Ref) && Durability == other.Durability;
    }
}