using MemoryPack;
using MTS.Game.BaseSkill;
using MTS.Game.Gacha;
using MTS.ND;

namespace MTS.Network.Models
{
    [MemoryPackable]

    public partial class GachaModel
    {
        public ETypeGacha TypeGacha { get; set; }
        public bool IsGacha10 { get; set; }
        public ETypeSkill TypeSkill { get; set; }
    }
}
