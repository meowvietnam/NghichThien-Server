using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class SetEquipmentNameResult : IMemorySerializable<SetEquipmentNameResult>
{
    public ushort SelfId { get; set; }
    public ushort Id { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Name {get; set;}
    public void Deserialize(IReader reader, ref SetEquipmentNameResult value)
    {
        value.Success = reader.Read<bool>();
        value.Message = reader.Read();
        value.SelfId = reader.Read<ushort>();
        value.Id = reader.Read<ushort>();
        value.Name = reader.Read();
    }

    public int ByteSize()
    {
        var size = Success.ByteSize() + Message.ByteSize();
        size += SelfId.ByteSize() + Id.ByteSize() + Name.ByteSize();
        return size;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        writer.Write(Message);
        writer.Write(SelfId);
        writer.Write(Id);
        writer.Write(Name);
    }
}