using MemoryPack;
using MTS.Game.Boss;

namespace MTS.Network.Models
{
[MemoryPackable]
public partial class BossStatus
{
    public bool Success { get; set; }
    public int RequestNodeId { get; set; }
    public long SpawnId { get; set; }
    public int NodeId { get; set; }
    public ETypeVisualChar SkinId { get; set; }
    public CharacterSnapshot Boss { get; set; }
    public bool IsAlive { get; set; }
    public int FightingCount { get; set; }
    public int RemainingAttempts { get; set; }
    public long DamageDone { get; set; }
    public float TamPhamChance { get; set; }
    public float TuPhamChance { get; set; }
    public float NguPhamChance { get; set; }
    public float LucPhamChance { get; set; }
    public string ScheduleText { get; set; }
}
}
