using MemoryPack;

namespace MTS.Network.Models
{
[MemoryPackable]
public partial class JoinBossResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public BossStatus Status { get; set; }
    public CharacterSnapshot Player { get; set; }
}
}
