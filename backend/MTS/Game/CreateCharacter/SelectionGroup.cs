using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class SelectionGroup : IMemorySerializable<SelectionGroup>
{
    public int Id { get; private set; }
    public List<int> Items { get; set; }
    public SelectionGroup(int id) {
        Id = id;
        Items = new List<int>();
    }
    public SelectionGroup()
    {
    }
    public int ByteSize()
    {
        return Id.ByteSize() + Items.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.WriteArray(Items);
    }
    public void Deserialize(IReader reader, ref SelectionGroup value)
    {
        value.Id = reader.Read<int>();
        value.Items = reader.ReadArray<int>().ToList();
    }
}