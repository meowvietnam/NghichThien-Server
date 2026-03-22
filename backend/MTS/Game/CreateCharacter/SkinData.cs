using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class SkinData : IMemorySerializable<SkinData>
{
    public string Name { get; private set; }
    public SkinData()
    {
    }
    public void Deserialize(IReader reader, ref SkinData value)
    {
        value.Name = reader.Read();
    }

    public int ByteSize()
    {
        return Name.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Name);
    }
}