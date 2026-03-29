using Network;
using Network.Extensions;

namespace MTS.Models;
public class FurnaceData : IMemorySerializable<FurnaceData>
{
    public int SelfId { get; set; }
    public ItemRef Ref { get; set; }
    public int ByteSize()
    {
        return SelfId.ByteSize() + Ref.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(SelfId);
        writer.Write(Ref);
    }

    public void Deserialize(IReader reader, ref FurnaceData value)
    {
        value.SelfId = reader.Read<int>();
        value.Ref = reader.Read<ItemRef>();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SelfId, Ref);
    }
}