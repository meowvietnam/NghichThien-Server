using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;
public class SkinResources : IMemorySerializable<SkinResources>
{
    public List<Color> Colors { get; private set; }
    public List<GenderSkinGroup> GenderSkinGroups { get; private set; }
    public int ByteSize()
    {
        return Colors.ByteSize() + GenderSkinGroups.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.WriteArray(Colors);
        writer.WriteArray(GenderSkinGroups);
    }
    public void Deserialize(IReader reader, ref SkinResources value)
    {
        value.Colors = reader.ReadArray<Color>().ToList();
        value.GenderSkinGroups = reader.ReadArray<GenderSkinGroup>().ToList();
    }
}