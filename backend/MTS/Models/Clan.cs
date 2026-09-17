using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
namespace MTS.Models;
[MemoryPackable]
public partial record Clan
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizeName { get; set; } = string.Empty;
    public int NodeId { get; set; }
    public string? ImageUrl { get; set; }
    public long Funds { get; set; }
    public long Hon { get; set; }
    public long MaxHon { get; set; }
    public int HonLimitLevel { get; set; } = 1;
    public long SoulEssence { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Announcement { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; }
    public int Level { get; set; }
    public long Exp { get; set; }
    public int LeaderCharId { get; set; }
    // Mốc thời gian gần nhất có người được PHONG LÊN từng chức vụ độc quyền này (không tính
    // giáng chức về Member); dùng để tính cooldown trước khi phong lại đúng chức vụ đó.
    public DateTime LastViceLeaderAssignedAt { get; set; }
    public DateTime LastElderAssignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ClanMember> Members { get; set; } = new();
    public List<ClanJoinRequest> JoinRequests { get; set; } = new();
    public List<ClanShopItem> ShopItems { get; set; } = new();
    [MemoryPackIgnore]
    public List<ClanInheritanceEffect> InheritanceEffects { get; set; } = new();
    [NotMapped]
    public List<ClanInheritanceEffect> StartTurnEffects { get; set; } = new();
    [NotMapped]
    public List<ClanInheritanceEffect> EndTurnEffects { get; set; } = new();

    public void RefreshInheritanceViews()
    {
        StartTurnEffects = InheritanceEffects
            .Where(x => x.Trigger == ClanInheritanceTrigger.StartTurn)
            .OrderBy(x => x.SlotIndex)
            .ToList();
        EndTurnEffects = InheritanceEffects
            .Where(x => x.Trigger == ClanInheritanceTrigger.EndTurn)
            .OrderBy(x => x.SlotIndex)
            .ToList();
    }

    public Clan CloneAsView()
    {
        return this with
        {
            JoinRequests = new()
        };
    }

}
