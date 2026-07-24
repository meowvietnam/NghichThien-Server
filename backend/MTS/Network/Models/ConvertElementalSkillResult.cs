using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ConvertElementalSkillResult : Result
    {
        public HashSet<ItemRef> ItemConverts { get; set; }
        public ItemRef ItemResult { get; set; }
    }
}