using MTS.Game.BaseSkill;
using MTS.Game.Resources;
using MTS.Models;
using MTS.Network.Models;
using Network;
using Network.Extensions;

namespace MTS.ND;

public class NgoDao : BaseData, IMemorySerializable<NgoDao>
{
    public int Require { get; set; }
    public ETypeSkill[] ETypeRequires { get; set; }
    public override int ByteSize()
    {
        var byteSize = base.ByteSize();
        return byteSize + Require.ByteSize() + ETypeRequires.ByteSize(); 
    }

    public virtual NgoDaoBuff Buff(CharacterData charData)
    {
        return new NgoDaoBuff();
    }

    public virtual void UnBuff(CharacterData charData)
    {
        
    }
    public void Deserialize(IReader reader, ref NgoDao value)
    {
        BaseData baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
        value.Require = reader.Read<int>();
        value.ETypeRequires = reader.ReadArray<ETypeSkill>();
    }
    public override void Serialize(IWriter writer)
    {
        base.Serialize(writer);
        writer.Write(Require);
        writer.WriteArray(ETypeRequires);
    }
}