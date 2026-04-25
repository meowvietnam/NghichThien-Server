using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class ChangeSceneResult : IMemorySerializable<ChangeSceneResult>
{
    public bool Success { get; set; }
    public string SceneName { get; set; }
    public string Message { get; set; }

    public void Deserialize(IReader reader, ref ChangeSceneResult value)
    {
        var success = reader.Read<bool>();
        value.Success = success;
        if (!success)
        {
            value.Message = reader.Read();
        }
        else
        {
            value.SceneName = reader.Read();
        }
    }

    public int ByteSize()
    {
        if (Success)
        {
            return Success.ByteSize() + SceneName.ByteSize();
        }
        else
        {
            return SceneName.ByteSize() + Message.ByteSize();
        }
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (Success)
        {
            writer.Write(SceneName);
        }
        else
        {
            writer.Write(Message);
        }
    }
}