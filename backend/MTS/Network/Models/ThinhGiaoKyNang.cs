using MemoryPack;
using MTS.Game.Combat;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ThinhGiaoKyNang
    {
        public int IdChar { get; set; }
        public int IdSkill { get; set; }
        public ItemType TypeSkill { get; set; }
    }

}
