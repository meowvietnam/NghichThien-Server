using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class CompleteMainQuestResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }    
        public int CurrentMainQuest { get; set; }
        public RewardItems RewardItems { get; set; }
        public int Money { get; set; }
        public int MoneyVip { get; set; }
    }
}
