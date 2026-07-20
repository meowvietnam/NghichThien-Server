using MTS.Game.BaseSkill;
using MTS.Game.Resources;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP;

public class CongPhap : BaseSkill, IMemorySerializable<CongPhap>
{
    public int TocDoTuLuyenBase { get; set; }
    public bool IsDonThuat { get; set; }
    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize + TocDoTuLuyenBase.ByteSize() + IsDonThuat.ByteSize(); 
    }
    public virtual void EquipCongPhap(CharacterData statsCurrent , int level)
    {

    }
    public virtual void UnEquipCongPhap(CharacterData statsCurrent, int level)
    {

    }
    public virtual void UpgradeCongPhapSuccess(CharacterData statsCurrent, int level)
    {
        
    }
    public virtual void LearnCongPhapSuccess(CharacterData statsCurrent, int level)
    {
        
    }
    public void Deserialize(IReader reader, ref CongPhap value)
    {
        BaseSkill baseValue = value;
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

    public int GetTocDoTuLuyen(int level)
    {
        return level * TocDoTuLuyenBase;
    }
}