using MTS.ND;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class LearnNgoDaoResult : IMemorySerializable<LearnNgoDaoResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public NgoDaoBuff Buff { get; set; }
    public MTS.Models.NgoDao NgoDao { get; set; }
    public int NewNgoDaoDiem { get; set; }
    public LearnNgoDaoResult()
    {
        
    }
    public void Deserialize(IReader reader, ref LearnNgoDaoResult value)
    {
        var success = reader.Read<bool>();
        value.Success = success;
        if (success)
        {
            value.Buff = reader.Read<NgoDaoBuff>();
            value.NgoDao = reader.Read<MTS.Models.NgoDao>();
            value.NewNgoDaoDiem = reader.Read<int>();
        }
        else
        {
            value.Message = reader.Read();
        }
    }

    public int ByteSize()
    {
        var byteSize = Success.ByteSize();
        var eByteSize = Success ? Buff.ByteSize() + NgoDao.ByteSize() + NewNgoDaoDiem.ByteSize() : Message.ByteSize();
        return byteSize + eByteSize;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (Success)
        {
            writer.Write(Buff);
            writer.Write(NgoDao);
            writer.Write(NewNgoDaoDiem);
        }
        else
        {
            writer.Write(Message);
        }
    }
}