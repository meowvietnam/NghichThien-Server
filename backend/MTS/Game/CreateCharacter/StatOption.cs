using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class StatOption : IMemorySerializable<StatOption>
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public List<StatModifier> Modifiers { get; private set; }
    public string Effective { get; private set; }
    public string Description { get; private set; }
    public int ByteSize()
    {
        return Id.ByteSize() + Name.ByteSize() + Modifiers.ByteSize() + Effective.ByteSize() + Description.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name);
        writer.WriteArray(Modifiers);
        writer.Write(Effective);
        writer.Write(Description);
    }
    public void Deserialize(IReader reader, ref StatOption value)
    {
        value.Id =  reader.Read<int>();
        value.Name = reader.Read();
        value.Modifiers = reader.ReadArray<StatModifier>().ToList();
        value.Effective = reader.Read();
        value.Description = reader.Read();
    }
}