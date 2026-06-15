using MemoryPack;

namespace MTS.Game.Snapshot
{
    [MemoryPackable]
    public partial class NpcData
    {
        public CharacterStatsSecureSnapshot CharStats { get; set; }
        public ETypeCombo ComboType { get; set; }
        [MemoryPackIgnore]
        private ComboWithType comboChar;
        [MemoryPackIgnore]
        public ComboWithType ComboChar
        {
            get
            {
                if (comboChar == null)
                {
                    comboChar = ComboSkillResource.Instance
                        .ComboNpc.First(x => x.TypeCombo == ComboType);
                }
                return comboChar;
            }
        }
    }
}