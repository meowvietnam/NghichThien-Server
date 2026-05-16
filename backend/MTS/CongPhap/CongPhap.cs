using MTS.Game.Resources;
using MTS.Models;
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
    public virtual void EquipCongPhap(CharacterData statsCurrent , int daiCanhGioiCongPhap)
    {

    }
    public virtual void UnEquipCongPhap(CharacterData statsCurrent, int daiCanhGioiCongPhap)
    {

    }
    public virtual void UpgradeCongPhapSuccess(CharacterData statsCurrent, int daiCanhGioiCongPhap)
    {
        UnEquipCongPhap(statsCurrent, daiCanhGioiCongPhap);
        EquipCongPhap(statsCurrent, daiCanhGioiCongPhap);
    }
    public virtual void LearnCongPhapSuccess(CharacterData statsCurrent, int daiCanhGioiCongPhap)
    {
        
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