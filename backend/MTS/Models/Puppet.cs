using MTS.Game.TMC;

namespace MTS.Models;

/// <summary>
/// Con rối phòng thủ của tông môn trong Tông môn chiến. Rối không có CharacterData riêng:
/// chỉ số chiến đấu được dựng từ nhân vật mẫu "Rối" (TcpServer.Scarecrow) khi ghép trận.
/// </summary>
public class Puppet
{
    public int Id { get; set; }
    public int ClanId { get; set; }
    public int Level { get; set; } = 1;
    /// <summary>Thành phần ngẫu nhiên sinh một lần khi tạo, lưu bền để công thức HP rối dùng lại.</summary>
    public int RandomSeed { get; set; }
    public int NodeId { get; set; }
    public int Hp { get; set; }
    public int HpMax { get; set; }
    public DateTime LastHpRecoveryAt { get; set; }
    public WarUnitState State { get; set; }
    /// <summary>Các node còn phải đi, không gồm node hiện tại. Lưu dạng "12,15,18".</summary>
    public string Route { get; set; } = string.Empty;
    public DateTime? NextArrivalAt { get; set; }
    public DateTime ArrivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
