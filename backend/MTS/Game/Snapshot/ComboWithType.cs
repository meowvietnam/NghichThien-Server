using MemoryPack;
using MTS.Game.BaseSkill;
using MTS.Models;

namespace MTS.Game.Snapshot
{
    [MemoryPackable]
    public partial class ComboWithType
    {
        public string NameCombo { get; set; }
        public Combo[] ComboWithDaiCanhGioi  { get; set; }
        public ETypeSkill[] ETypeSkill { get; set; }
        public ETypeCombo TypeCombo { get; set; }
        public EMonPhai MonPhai { get; set; }

    }
}