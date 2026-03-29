using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class GetInventoryResult : IMemorySerializable<GetInventoryResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public InventoryManager InventoryManager { get; set; }
    public int ByteSize()
    {
        if (Success)
        {
            return Success.ByteSize() + InventoryManager.ByteSize();
        }
        else
        {
            return Success.ByteSize() + Message.ByteSize();
        }
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (Success)
        {
            writer.Write(InventoryManager);
        }
        else
        {
            writer.Write(Message);
        }
    }
    public void Deserialize(IReader reader, ref GetInventoryResult value)
    {
        var success = reader.Read<bool>();
        value.Success = success;
        if (success)
        {
            value.InventoryManager = reader.Read<InventoryManager>();
        }
        else
        {
            value.Message = reader.Read();
        }
    }
}