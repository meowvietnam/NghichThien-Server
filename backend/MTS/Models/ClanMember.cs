namespace MTS.Models;

public class ClanMember
{
    public int CharId { get; set; }
    public int ClanId { get; set; }
    // Leader is derived from Clan.LeaderCharId; never persist Leader here.
    public ClanRole Role { get; set; } = ClanRole.Member;
    public int? ActiveInheritanceId { get; set; }
    public long TotalContribution { get; set; }
    public long AvailableContribution { get; set; }
    public DateTime JoinedAt { get; set; }
    public long Version { get; set; } = 1;

    public bool HasPermission(ClanPermission permission) =>
        permission != ClanPermission.None &&
        (ClanRolePermissions.GetPermissions(Role) & permission) == permission;
}
