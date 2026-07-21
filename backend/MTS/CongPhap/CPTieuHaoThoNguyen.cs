using MTS.Exceptions;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP;

public class CPTieuHaoThoNguyen : CongPhap, IMemorySerializable<CPTieuHaoThoNguyen>
{
    public int[] tieuHao =
    [
        2,4,6,8,10
    ];

    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize + tieuHao.ByteSize();
    }

    public override void LearnCongPhapSuccess(CharacterData statsCurrent, int level)
    {
        TieuHaoThoNguyen(statsCurrent, level, tieuHao);
    }

    public override void UpgradeCongPhapSuccess(CharacterData statsCurrent, int level)
    {
        TieuHaoThoNguyen(statsCurrent, level, tieuHao);
    }

    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
        writer.WriteArray(tieuHao);
    }
    public void Deserialize(IReader reader, ref CPTieuHaoThoNguyen value)
    {
        CongPhap baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
        value.tieuHao = reader.ReadArray<int>();
    }
}