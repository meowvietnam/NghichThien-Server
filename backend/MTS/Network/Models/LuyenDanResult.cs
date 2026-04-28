using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class LuyenDanResult : IMemorySerializable<LuyenDanResult>
{
    public bool Success { get; set; }
    public string Message { get;  set; }
    public List<ItemRef> ItemRefs { get; set; }
    public TimeSpan NewTuoiTac { get; set; }
    public InventoryModify Modify { get; set; }


    public int ByteSize()
    {
        var baseSize = Success.ByteSize() + Message.ByteSize();
        if (!Success)
        {
            return baseSize;
        }
        return baseSize + ItemRefs.ByteSize() + NewTuoiTac.ByteSize() + Modify.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        writer.Write(Message);
        if (Success)
        {
            writer.WriteArray(ItemRefs);
            writer.Write(NewTuoiTac);
            writer.Write(Modify);
        }
    }
    public void Deserialize(IReader reader, ref LuyenDanResult value)
    {
        value.Success = reader.Read<bool>();
        value.Message = reader.Read();
        if (value.Success)
        {
            value.ItemRefs = reader.ReadArray<ItemRef>().ToList();
            value.NewTuoiTac = reader.Read<TimeSpan>();
            value.Modify = reader.Read<InventoryModify>();
        }
    }
}