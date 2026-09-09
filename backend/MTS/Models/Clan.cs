namespace MTS.Models;

public class Clan
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int EmblemId { get; set; }
    public string Description { get; set; } = "";
    public string Announcement { get; set; } = "";
    // The AllMaps node selected at creation. The client only receives it when it is a member.
    public int? CastleNodeId { get; set; }
    public long JadeFunds { get; set; }
    public int LeaderCharId { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public long Funds { get; set; }
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; }
    public ClanJoinPolicy JoinPolicy { get; set; } = ClanJoinPolicy.Approval;
    public int MinLevel { get; set; }
    public ClanStatus Status { get; set; }
    public long Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DissolvedAt { get; set; }
}
