using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class CompleteSideQuest
    {
        public List<int> ListIdNpc { get; set; }
    }
}
