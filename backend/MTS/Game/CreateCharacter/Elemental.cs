using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class Elemental : IMemorySerializable<Elemental>
{
    public int Id { get; private set; }
    public ElementalType ElementalType { get; private set; }
    public string Name { get; private set; }
    public StatType StatType => (StatType)(ElementalType + 1);

    public int ByteSize()
    {
        return Id.ByteSize() + ElementalType.ByteSize() + Name.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(ElementalType);
        writer.Write(Name);
    }
    public void Deserialize(IReader reader, ref Elemental value)
    {
        value.Id = reader.Read<int>();
        value.ElementalType = reader.Read<ElementalType>();
        value.Name = reader.Read();
    }
}