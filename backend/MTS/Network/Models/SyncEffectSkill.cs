using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class SyncEffectSkill
{
    public int CharId { get; set; }
    public int IdSkill { get; set; }
    public bool IsThanThong { get; set; }
}
