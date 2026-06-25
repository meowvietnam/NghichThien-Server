using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class TangQuaNpc
    {
        public int IdChar { get; set; }
        public ItemRef ItemRef { get; set; }

    }
   
}
