using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LinhNgo
    {
        public int Id { get; set; }
        public ItemType Type { get; set; }
    }
}