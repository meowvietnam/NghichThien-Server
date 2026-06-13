using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class WatchAdsForEnergyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int NewEnergy { get; set; }
    }
}
