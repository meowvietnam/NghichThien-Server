using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LuyenDanConfig
    {
        public float MinRate { get; set; }
        public float RatePerTier { get; set; }
    }
}