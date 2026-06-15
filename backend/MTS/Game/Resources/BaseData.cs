using Network;
using Network.Extensions;

namespace MTS.Game.Resources;

public class BaseData : IMemorySerializable<BaseData>
{
    public int  Id { get; private set; }
    public string Name { get; private set; }
    public void Deserialize(IReader reader, ref BaseData value)
    {
        value.Id = reader.Read<int>();
        value.Name = reader.Read();
    }
    public virtual int ByteSize()
    {
        return Id.ByteSize() + Name.ByteSize();
    }
    public virtual void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Name);
    }
}