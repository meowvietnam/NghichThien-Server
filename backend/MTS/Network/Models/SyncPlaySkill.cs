using MemoryPack;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class SyncPlaySkill
    {
        public int CharId { get; set; }
        public int SkillId { get; set; }
    }
}