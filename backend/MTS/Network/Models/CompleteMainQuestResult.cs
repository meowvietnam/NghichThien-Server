using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class CompleteMainQuestResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }    
        public int CurrentMainQuest { get; set; }
    }
}
