using MemoryPack;
using MTS.Game.Gacha;

namespace MTS.Network.Models
{
    [MemoryPackable]

    public partial class GachaModel
    {
        public ETypeGacha TypeGacha { get; set; }
        public bool IsGacha10 { get; set; }
    
    }
}
