using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ConvertElementalSkill
    {
        public HashSet<ItemRef> ItemConverts { get; set; }
    }
}