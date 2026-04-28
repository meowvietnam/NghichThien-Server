using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class LuyenKhiResult : IMemorySerializable<LuyenKhiResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Equipment Item { get; set; }
    public TimeSpan NewTuoiTac { get; set; }
    public InventoryModify Modify { get; set; }
    
    public void Deserialize(IReader reader, ref LuyenKhiResult value)
    {
        value.Success = reader.Read<bool>();
        value.Message = reader.Read();
        if (value.Success)
        {
            value.Item = reader.Read<Equipment>();
            value.NewTuoiTac = reader.Read<TimeSpan>();
            value.Modify = reader.Read<InventoryModify>();
        }
    }

    public int ByteSize()
    {
        var size = Success.ByteSize() + Message.ByteSize();
        if (Success)
        {
            size += Item.ByteSize() + NewTuoiTac.ByteSize() + Modify.ByteSize();
        }
        return size;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        writer.Write(Message);
        if (Success)
        {
            writer.Write(Item);
            writer.Write(NewTuoiTac);
            writer.Write(Modify);
        }
    }
}