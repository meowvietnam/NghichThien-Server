using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP;

public class NguKiemPhiHanh : CongPhap, IMemorySerializable<NguKiemPhiHanh>
{
    private int[] buffValues;
    public override void EquipCongPhap(CharacterData statsCurrent, int level)
    {
        var stats = statsCurrent.Stats;
        var newTocDoBay = stats.TocDoBay + buffValues[level - 1];
        stats.SetTocDoBay(newTocDoBay);
        statsCurrent.SetCanFly(true);
    }
    public override void UnEquipCongPhap(CharacterData statsCurrent, int level)
    {
        var stats = statsCurrent.Stats;
        var newTocDoBay = stats.TocDoBay - buffValues[level - 1];
        stats.SetTocDoBay(newTocDoBay);
        statsCurrent.SetCanFly(false);
    }
    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize + buffValues.ByteSize();
    }
    public void Deserialize(IReader reader, ref NguKiemPhiHanh value)
    {
        CongPhap baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
        value.buffValues = reader.ReadArray<int>();
    }
    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
        writer.WriteArray(buffValues);
    }
}