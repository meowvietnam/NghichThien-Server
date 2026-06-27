using MemoryPack;
using MTS.Game.Combat;
namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class ResultCombat
    {
        public List<int> ListIdThanThong { get; set; }
        public ETypeCombat TypeCombat { get; set; }
        public int CurrentHPUser { get; set; }
        public int CurrentLevelEnemy { get; set; }
        public ETypeResultCombat TypeResultCombat { get; set; }

    }
}
