using System.ComponentModel.DataAnnotations.Schema;
using MTS.Game.TMC;
using Newtonsoft.Json;

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
    /// <summary>Độ bền: số lần còn được chết (HP về 0). Về 0 thì rối bị xoá; nâng cấp hồi đầy.</summary>
    public int Durability { get; set; } = WarConfig.PuppetMaxDurability;
    /// <summary>Thành phần ngẫu nhiên sinh một lần khi tạo, lưu bền để công thức HP rối dùng lại.</summary>
    public int RandomSeed { get; set; }
    public int NodeId { get; set; }
    public int Hp { get; set; }
    public int HpMax { get; set; }
    /// <summary>Độn tốc (TocDoBay) của rối = (level × 10)% độn tốc người tạo/nâng cấp, chốt lúc tạo/nâng.</summary>
    public int DonToc { get; set; }
    public DateTime LastHpRecoveryAt { get; set; }
    public WarUnitState State { get; set; }
    /// <summary>Các node còn phải đi, không gồm node hiện tại. Lưu dạng "12,15,18".</summary>
    public string Route { get; set; } = string.Empty;
    public DateTime? NextArrivalAt { get; set; }
    public DateTime ArrivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Ngoại hình random một lần khi tạo rối, lưu bền (JSON của Skins/Colors).</summary>
    public Gender Gender { get; set; }
    public string AppearanceJson { get; set; } = string.Empty;

    [NotMapped]
    [JsonIgnore]
    public Appearance Appearance
    {
        get
        {
            if (cachedAppearance != null) return cachedAppearance;
            if (string.IsNullOrWhiteSpace(AppearanceJson)) return cachedAppearance = new Appearance();
            try
            {
                cachedAppearance = JsonConvert.DeserializeObject<Appearance>(AppearanceJson) ?? new Appearance();
            }
            catch (JsonException)
            {
                cachedAppearance = new Appearance();
            }

            return cachedAppearance;
        }
        set
        {
            cachedAppearance = value;
            AppearanceJson = JsonConvert.SerializeObject(value);
        }
    }

    private Appearance? cachedAppearance;
}
