using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class ColorGroup : IMemorySerializable<ColorGroup>
{
    public string Id { get; private set; }
    public List<string> CategoryIds { get; private set; }
    public Color DefaultColor { get; private set; }
    public int ByteSize()
    {
        return Id.ByteSize() + CategoryIds.ByteSize() + DefaultColor.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.WriteArray(CategoryIds);
        writer.Write(DefaultColor);
    }
    public void Deserialize(IReader reader, ref ColorGroup value)
    {
        value.Id = reader.Read();
        value.CategoryIds = reader.ReadArray().ToList();
        value.DefaultColor = reader.Read<Color>();
    }
}