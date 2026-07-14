using MemoryPack;

namespace MTS.Network.Models
{

    [MemoryPackable]
    public partial class CompleteSideQuestResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int MoneyVipRandom { get; set; }
        public int CurrentSideQuest { get; set; }
        public int LinhThachRandom { get; set; }
        public int NewTamCanh { get; set; }
    }
}
