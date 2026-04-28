using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class RewardItems : IMemorySerializable<RewardItems>
{
    public List<FurnaceData> Furnaces { get; set; } = new();
    public List<ItemRef> OtherItems { get; set; } = new();
    public void Deserialize(IReader reader, ref RewardItems value)
    {
        value.Furnaces = reader.ReadArray<FurnaceData>().ToList();
        value.OtherItems = reader.ReadArray<ItemRef>().ToList();
    }

    public int ByteSize()
    {
        return Furnaces.ByteSize() + OtherItems.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.WriteArray(Furnaces);
        writer.WriteArray(OtherItems);
    }
}