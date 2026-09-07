using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ConvertDanLuResult : Result
    {
        public List<ushort> FurnaceSelfIds { get; set; }
        public ItemRef ItemResult { get; set; }
    }
}
