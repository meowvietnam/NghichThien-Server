namespace MTS.Models;
public static class ClanRolePermissions
{
    public static ClanPermission GetPermissions(ClanRole role) => role switch
    {
        ClanRole.Leader => ClanPermission.ReviewApplications | ClanPermission.KickMembers |
            ClanPermission.EditSettings | ClanPermission.ChangeRoles | ClanPermission.TransferLeadership | ClanPermission.Dissolve,
        ClanRole.Deputy => ClanPermission.ReviewApplications | ClanPermission.KickMembers | ClanPermission.EditSettings,
        ClanRole.Elder => ClanPermission.ReviewApplications,
        _ => ClanPermission.None
    };
}
