using MTS.Converter;
using Network;
using Network.Extensions;

namespace MTS.Models;
[Newtonsoft.Json.JsonConverter(typeof(FurnaceDataConverter))]
public class FurnaceData : IMemorySerializable<FurnaceData>
{
    public int SelfId { get; set; }
    public ItemRef Ref { get; set; }
    public int Durability { get; set; }
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
        value.SelfId = reader.Read<int>();
        value.Ref = reader.Read<ItemRef>();
        value.Durability = reader.Read<int>();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SelfId, Ref);
    }
}