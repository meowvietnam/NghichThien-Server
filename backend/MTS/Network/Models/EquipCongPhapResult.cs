using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class EquipCongPhapResult : IMemorySerializable<EquipCongPhapResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ETypeSlotCP Slot { get; set; }
    public ushort Id { get; set; }

    public void Deserialize(IReader reader, ref EquipCongPhapResult value)
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
            value.Id = reader.Read<ushort>();
        }
    }

    public int ByteSize()
    {
        var baseSize = Success.ByteSize();
        var exSize = !Success ? Message.ByteSize() : Slot.ByteSize() + Id.ByteSize();
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
            writer.Write(Id);
        }
    }
}