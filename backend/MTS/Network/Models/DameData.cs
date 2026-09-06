using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class DameData
    {
        public int Dame { get; set; }
        public int DameHoTrao { get; set; }
        public bool IsNeTranh { get; set; }
        public bool IsDef { get; set; }
    }
}