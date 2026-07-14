using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class RewardItems 
{
    public List<FurnaceData> Furnaces { get; set; } = new();
    public List<ItemRef> OtherItems { get; set; } = new();
}