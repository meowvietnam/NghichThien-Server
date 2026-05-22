using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class EquipThanThongResult : IMemorySerializable<EquipThanThongResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ThanThongEquip ThanThong { get; set; }

    public void Deserialize(IReader reader, ref EquipThanThongResult value)
    {
        var success = reader.Read<bool>();
        Success = success;
        if (success)
        {
            value.ThanThong = reader.Read<ThanThongEquip>();
        }
        else
        {
            value.Message = reader.Read();
        }
    }

    public int ByteSize()
    {
        var baseSize = Success.ByteSize();
        var exSize = Success ? ThanThong.ByteSize() : Message.ByteSize();
        return baseSize + exSize;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (Success)
        {
            writer.Write(ThanThong);
        }
        else 
        {
            writer.Write(Message);
        }
    }
}