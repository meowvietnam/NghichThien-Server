using System.ComponentModel.DataAnnotations;
using MTS.Game.TMC;

namespace MTS.Models;

/// <summary>Nhật ký trận Tông môn chiến; dùng để chống báo kết quả lặp và dọn trận treo sau restart.</summary>
public class WarBattle
{
    [Key]
    public long Id { get; set; }
    public int NodeId { get; set; }
    public bool IsPvp { get; set; }
    public int AttackerCharId { get; set; }
    public int AttackerClanId { get; set; }
    public int? DefenderCharId { get; set; }
    public int? DefenderPuppetId { get; set; }
    public int DefenderClanId { get; set; }
    public int AttackerHpBefore { get; set; }
    public int DefenderHpBefore { get; set; }
    public int AttackerHpAfter { get; set; }
    public int DefenderHpAfter { get; set; }
    public WarBattleState State { get; set; }
    public WarBattleOutcome Outcome { get; set; }
    public WarBattleEndReason EndReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
