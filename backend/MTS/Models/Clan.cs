using System.ComponentModel.DataAnnotations;
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
    public string Description { get; set; } = string.Empty;
    public string Announcement { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; }
    public int Level { get; set; }
    public long Exp { get; set; }
    public int LeaderCharId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ClanMember> Members { get; set; } = new();
    public List<ClanJoinRequest> JoinRequests { get; set; } = new();
    public List<ClanShopItem> ShopItems { get; set; } = new();

    public Clan CloneAsView()
    {
        return this with
        {
            JoinRequests = new()
        };
    }

}
