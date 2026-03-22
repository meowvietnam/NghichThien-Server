using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class GenderSkinGroup : IMemorySerializable<GenderSkinGroup>
{
    public Gender Gender { get; private set; }
    public List<SkinGroup> SkinGroups { get; private set; }

    public int ByteSize()
    {
        return Gender.ByteSize() + SkinGroups.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Gender);
        writer.WriteArray(SkinGroups);
    }

    public void Deserialize(IReader reader, ref GenderSkinGroup value)
    {
        value.Gender = reader.Read<Gender>();
        value.SkinGroups = reader.ReadArray<SkinGroup>().ToList();
    }
}