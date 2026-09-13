using Network;
using Network.Extensions;

namespace MTS.CP;

public class UHonQuyet : CongPhap, IMemorySerializable<UHonQuyet>
{
    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize;
    }

    public void Deserialize(IReader reader, ref UHonQuyet value)
    {
        CongPhap baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }

    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
    }
}
