using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class ApplyStatsResult : IMemorySerializable<ApplyStatsResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public void Deserialize(IReader reader, ref ApplyStatsResult value)
    {
        value.Success = reader.Read<bool>();
        if (!value.Success)
        {
            value.Message = reader.Read();
        }
    }
    public int ByteSize()
    {
        if (Success)
        {
            return Success.ByteSize();
        }
        else
        {
            return Success.ByteSize() + Message.ByteSize();
        }
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (!Success)
        {
            writer.Write(Message);
        }
    }
}