using System.Security.Cryptography.Xml;
using Network;
using Network.Extensions;

namespace MTS.Game.CreateCharacter;

public class SelectionStepData : StepData, IMemorySerializable<SelectionStepData>
{
    public List<StatOptionGroup> Groups { get; private set; }


    public override int ByteSize()
    {
        return StepType.ByteSize() + Id.ByteSize() + Title.ByteSize() + Description.ByteSize() + Groups.ByteSize();
    }

    public override void Serialize(IWriter writer)
    {
        writer.Write(StepType);
        writer.Write(Id);
        writer.Write(Title);
        writer.Write(Description);
        writer.WriteArray(Groups);
    }

    public void Deserialize(IReader reader, ref SelectionStepData value)
    {
        value.StepType = StepType.Selection;
        value.Id = reader.Read<int>();
        value.Title = reader.Read();
        value.Description = reader.Read();
        value.Groups = reader.ReadArray<StatOptionGroup>().ToList();
    }
}