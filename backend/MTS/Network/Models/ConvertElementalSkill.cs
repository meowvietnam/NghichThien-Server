using MemoryPack;
using MTS.Game.BaseSkill;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ConvertElementalSkill
    {
        public ETypeSkill TypeSkill { get; set; }
        public HashSet<ItemRef> ItemConverts { get; set; }
    }
}