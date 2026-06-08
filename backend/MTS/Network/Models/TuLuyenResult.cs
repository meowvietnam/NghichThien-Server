using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class TuLuyenResult : IMemorySerializable<TuLuyenResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public bool HasLevelUpdated { get; set; }
    public int CurrentExp { get; set; }
    public LevelUpdated LevelUpdated { get; set; }
    public void Deserialize(IReader reader, ref TuLuyenResult value)
    {
        var success = reader.Read<bool>();
        value.Success = success;
        if (!success)
        {
            value.Message = reader.Read();
            return;
        }
        var hasLevelUpdated = reader.Read<bool>();
        value.HasLevelUpdated = hasLevelUpdated;
        if (!hasLevelUpdated)
        {
            value.CurrentExp = reader.Read<int>();
            return;
        }
        value.LevelUpdated = reader.Read<LevelUpdated>();
    }

    public int ByteSize()
    {
        var baseSize = Success.ByteSize();
        var exSize = 0;
        if (!Success)
        {
            exSize = Message.ByteSize();
        }
        else
        {
            if (!HasLevelUpdated)
            {
                exSize = CurrentExp.ByteSize();
            }
            else
            {
                exSize = LevelUpdated.ByteSize();
            }
        }
        return baseSize + exSize;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (!Success)
        {
            writer.Write(Message);
            return;
        }
        if (!HasLevelUpdated)
        {
            writer.Write(CurrentExp);
            return;
        }
        writer.Write(LevelUpdated);
    }
}