using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class ElementalStepData : StepData, IMemorySerializable<ElementalStepData>
{
    public int GroupId { get; private set; }
    public List<Elemental> Elementals { get; private set; }

    public override int ByteSize()
    {
        return StepType.ByteSize() + Id.ByteSize() + Title.ByteSize() + Description.ByteSize() + GroupId.ByteSize() + Elementals.ByteSize();
    }
    public override void Serialize(IWriter writer)
    {
        writer.Write(StepType);
        writer.Write(Id);
        writer.Write(Title);
        writer.Write(Description);
        writer.Write(GroupId);
        writer.WriteArray(Elementals);
    }
    public void Deserialize(IReader reader, ref ElementalStepData value)
    {
        value.StepType = StepType.Elemental;
        value.Id = reader.Read<int>();
        value.Title = reader.Read();
        value.Description = reader.Read();
        value.GroupId = reader.Read<int>();
        value.Elementals = reader.ReadArray<Elemental>().ToList();
    }
}