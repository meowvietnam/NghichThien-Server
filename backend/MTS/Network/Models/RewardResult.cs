using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class RewardResult : IMemorySerializable<RewardResult>
{
    public List<int> Mails { get; set; }
    public RewardItems RewardItems { get; set; }
    public void Deserialize(IReader reader, ref RewardResult value)
    {
        value.Mails = reader.ReadArray<int>().ToList();
        value.RewardItems = reader.Read<RewardItems>();
    }

    public int ByteSize()
    {
        return Mails.ByteSize() + RewardItems.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.WriteArray(Mails);
        writer.Write(RewardItems);
    }
}