using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class UnequipThanThongResult : IMemorySerializable<UnequipThanThongResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ushort Id { get; set; }
    public void Deserialize(IReader reader, ref UnequipThanThongResult value)
    {
        var success = reader.Read<bool>();
        Success = success;
        if (success)
        {
            value.Id = reader.Read<ushort>();
        }
        else
        {
            value.Message = reader.Read();
        }
    }
    public int ByteSize()
    {
        var baseSize = Success.ByteSize();
        var exSize = Success ? Id.ByteSize() : Message.ByteSize();
        return baseSize + exSize;
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (Success)
        {
            writer.Write(Id);
        }
        else 
        {
            writer.Write(Message);
        }
    }
}
