using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class SkinCategory : IMemorySerializable<SkinCategory>
{
    public string Id { get; set; }
    public string Name { get; private set; }
    public List<SkinData> Skins { get; private set; }
    public SkinCategory()
    {
    }
    public SkinCategory(string name, List<SkinData> skins)
    {
        Name = name;
        Skins = skins;
    }
    public void Deserialize(IReader reader, ref SkinCategory value)
    {
        value.Id = reader.Read();
        value.Name = reader.Read();
        value.Skins = reader.ReadArray<SkinData>().ToList();
    }
    public int ByteSize()
    {
        return Id.ByteSize() + Name.ByteSize() + Skins.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name);
        writer.WriteArray(Skins);
    }
}