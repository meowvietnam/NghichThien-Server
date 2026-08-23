using MemoryPack;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.Gacha;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class GachaResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public RewardItems RewardItems { get; set; }
        public int NewTienNgoc { get; set; }
        public int Money { get; set; }
        public int MoneyVip { get; set; }
        public ETypeGacha Type { get; set; }
        public ETypeSkill Skill { get; set; }
        public int TotalGacha { get; set; }
    }
}