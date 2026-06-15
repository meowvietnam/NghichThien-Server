using MemoryPack;

namespace MTS.Game.Snapshot
{
    [MemoryPackable]
    public partial class ComboSkillResource
    {
        public ComboWithType[] ComboNpc { get; set; }
        [MemoryPackIgnore]
        public static ComboSkillResource Instance => GameConfig.ComboSkillResources;
    }
}