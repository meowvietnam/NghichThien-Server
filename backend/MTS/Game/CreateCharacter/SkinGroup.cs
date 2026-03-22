using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class SkinGroup : IMemorySerializable<SkinGroup>
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public List<SkinCategory> Categories { get; private set; }
    public List<ColorGroup> ColorGroups { get; private set; }

    public int ByteSize()
    {
        return Id.ByteSize() + Name.ByteSize() + Categories.ByteSize() + ColorGroups.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name);
        writer.WriteArray(Categories);
        writer.WriteArray(ColorGroups);
    }
    public void Deserialize(IReader reader, ref SkinGroup value)
    {
        value.Id = reader.Read();
        value.Name = reader.Read();
        value.Categories = reader.ReadArray<SkinCategory>().ToList();
        value.ColorGroups = reader.ReadArray<ColorGroup>().ToList();
    }
}