namespace MTS.Game.Boss;

// One row per spawn. HP, attempts and damage survive reconnects and server restarts.
public class BossProgress
{
    public long Id { get; set; }
    public byte[] BossBytes { get; set; } = [];
    public int NodeId { get; set; }
    public int Hp { get; set; }
    public string AttemptsJson { get; set; } = "{}";
    public string DamageJson { get; set; } = "{}";
    public int? KillerId { get; set; }
    public byte[] ParticipantRewards { get; set; } = [];
    public byte[] KillerRewards { get; set; } = [];
}
