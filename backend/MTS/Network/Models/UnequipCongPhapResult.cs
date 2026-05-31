using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class UnequipCongPhapResult : IMemorySerializable<UnequipCongPhapResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ETypeSlotCP Slot { get; set; }

    public void Deserialize(IReader reader, ref UnequipCongPhapResult value)
    {
        var success = reader.Read<bool>();
        value.Success = success;
        if (!success)
        {
            value.Message = reader.Read();
        }
        else
        {
            value.Slot = reader.Read<ETypeSlotCP>();
        }
    }

    public int ByteSize()
    {
        var baseSize = Success.ByteSize();
        var exSize = !Success ? Message.ByteSize() : Slot.ByteSize();
        return baseSize + exSize;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (!Success)
        {
            writer.Write(Message);
        }
        else
        {
            writer.Write(Slot);
        }
    }
}