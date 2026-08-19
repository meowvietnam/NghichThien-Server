using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class TrungSinhConfig
    {
        public int MoneyVip { get; set; }
        public int ThienPhu { get; set; }
        public int TargetLevel { get; set; }
        public int DaiCanhGioi { get; set; }
        public CharacterStats Stats { get; set; }
    }
}