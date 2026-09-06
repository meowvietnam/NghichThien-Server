namespace MTS.CP;
public class CuuUHuyetMaCong : CongPhap, IMemorySerializable<CuuUHuyetMaCong>
{
    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize;
    }
    public void Deserialize(IReader reader, ref CuuUHuyetMaCong value)
    {
        CongPhap baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }
    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
    }
}
