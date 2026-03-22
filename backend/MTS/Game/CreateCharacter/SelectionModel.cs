using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class SelectionModel : IMemorySerializable<SelectionModel>
{
    public int Id { get; private set; }
    public List<SelectionGroup> Groups { get; set; }

    public SelectionModel(int id)
    {
        Id = id;
        Groups = new List<SelectionGroup>();
    }
    public SelectionModel()
    {
    }
    public int ByteSize()
    {
        return Id.ByteSize() + Groups.ByteSize();
    }
    public void Deserialize(IReader reader, ref SelectionModel value)
    {
        value.Id = reader.Read<int>();
        value.Groups = reader.ReadArray<SelectionGroup>().ToList();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.WriteArray(Groups);
    }
}