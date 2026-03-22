using Network;

namespace MTS.Game.CreateCharacter;

public abstract class StepData : IMemorySerializable
{
    public StepType StepType { get; protected set; }
    public int Id { get; protected set; }
    public string Title { get; protected set; }
    public string Description { get; protected set; }
    public abstract int ByteSize();
    public abstract void Serialize(IWriter writer);
}