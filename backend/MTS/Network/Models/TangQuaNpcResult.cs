using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class TangQuaNpcResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int IdChar { get; set; }
        public int NewValueRelationship { get; set; }

    }

}
