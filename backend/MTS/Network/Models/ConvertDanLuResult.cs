using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ConvertDanLuResult : Result
    {
        /// <summary>The recipe ItemRefs that were consumed.</summary>
        public List<ItemRef> RecipeItems { get; set; }
        /// <summary>The đan lô (furnace) received as a result.</summary>
        public FurnaceData FurnaceResult { get; set; }
    }
}
