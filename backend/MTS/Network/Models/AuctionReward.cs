using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class AuctionReward
{
    public string Content { get; set; }
    public ItemRef Item { get; set; }
    public List<FurnaceData> FurnaceDatas { get; set; }
}