using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ConvertDanLuRequest
    {
        public List<ushort> FurnaceSelfIds { get; set; }
    }
}
