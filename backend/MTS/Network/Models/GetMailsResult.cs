using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class GetMailsResult : IMemorySerializable<GetMailsResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<Mail> Mails { get; set; }

    public void Deserialize(IReader reader, ref GetMailsResult value)
    {
        value.Success = reader.Read<bool>();
        if (!value.Success)
        {
            value.Message = reader.Read();
        }
        else
        {
            value.Mails = reader.ReadArray<Mail>().ToList();
        }
    }

    public int ByteSize()
    {
        if (Success)
        {
            return Success.ByteSize() + Mails.ByteSize();
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
        else
        {
            writer.WriteArray(Mails);
        }
    }
}