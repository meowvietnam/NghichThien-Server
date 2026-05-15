using MTS.Game.Resources;
using Network;
using Network.Extensions;

namespace MTS.CP;

public class CongPhap : BaseData, IMemorySerializable<CongPhap>
{
    public int TocDoTuLuyenBase { get; set; }
    public bool IsDonThuat { get; set; }
    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize + TocDoTuLuyenBase.ByteSize() + IsDonThuat.ByteSize(); 
    }

    public void Deserialize(IReader reader, ref CongPhap value)
    {
        BaseData baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
        value.TocDoTuLuyenBase = reader.Read<int>();
        value.IsDonThuat = reader.Read<bool>();
    }
    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
        writer.Write(TocDoTuLuyenBase);
        writer.Write(IsDonThuat);
    }
}