using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ConvertDanLuRequest
    {
        /// <summary>
        /// The recipe ItemRefs to consume (grouped by Id, Count indicates quantity used).
        /// Total count across all entries must equal 4.
        /// </summary>
        public List<ItemRef> RecipeItems { get; set; }
    }
}
