using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LikeDongPhuNotify
    {
        public int NewLike { get; set; }
    }
    [MemoryPackable]
    public partial class LikeDongPhuResult : Result
    {
        public int NewLike { get; set; }
        public bool CanLike { get; set; }
    }
}
