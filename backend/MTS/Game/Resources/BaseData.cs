using Network;
using Network.Extensions;

namespace MTS.Game.Resources;

public class BaseData : IMemorySerializable<BaseData>
{
    public int  Id { get; set; }
    public void Deserialize(IReader reader, ref BaseData value)
    {
        value.Id = reader.Read<int>();
    }
    public virtual int ByteSize()
    {
        return Id.ByteSize();
    }
    public virtual void Serialize(IWriter writer)
    {
        writer.Write(Id);
    }
}