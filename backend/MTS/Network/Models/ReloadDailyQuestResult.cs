using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ReloadDailyQuestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public EDailyQuest EDailyQuest { get; set; }
        public List<DailyQuest> DailyQuests { get; set; } = new();
    }
}