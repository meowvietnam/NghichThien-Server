using System.ComponentModel.DataAnnotations;
using MTS.Game.TMC;

namespace MTS.Models;

/// <summary>Trạng thái Tông môn chiến của một nhân vật, độc lập với Progression.IdCurrentNode và Stats.Hp.</summary>
public class WarParticipant
{
    [Key]
    public int CharId { get; set; }
    public int ClanId { get; set; }
    public int NodeId { get; set; }
    public int Hp { get; set; }
    public int HpMax { get; set; }
    public DateTime LastHpRecoveryAt { get; set; }
    public WarUnitState State { get; set; }
    /// <summary>Các node còn phải đi, không gồm node hiện tại. Lưu dạng "12,15,18".</summary>
    public string Route { get; set; } = string.Empty;
    public DateTime? NextArrivalAt { get; set; }
    /// <summary>Node xuất phát của cạnh đang đi; dùng làm điểm về khi logout giữa đường.</summary>
    public int LastSafeNodeId { get; set; }
    public DateTime ArrivedAt { get; set; }
    /// <summary>Lần gần nhất mở scene chiến (UTC). Điều kiện nhận thưởng tuần: vào scene ít nhất 1 lần trong tuần.</summary>
    public DateTime? LastEnterAt { get; set; }
}
