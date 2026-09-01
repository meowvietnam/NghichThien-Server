using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class PlantNotify
    {
        public PlantSeed PlantSeed { get; set; }
    }
    [MemoryPackable]
    public partial class PlantResult : Result
    {
        public Seed UpdatedSeed { get; set; }
        public PlantSeed PlantSeed { get; set; }
    }
}
