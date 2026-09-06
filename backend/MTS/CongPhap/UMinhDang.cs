namespace MTS.CP;
public class UMinhDang : CongPhap, IMemorySerializable<UMinhDang>
{
    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize;
    }
    public void Deserialize(IReader reader, ref UMinhDang value)
    {
        CongPhap baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }
    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
    }
}