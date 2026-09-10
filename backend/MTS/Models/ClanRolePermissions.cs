namespace MTS.Models;

public static class ClanRolePermissions
{
    public static ClanPermission GetPermissions(ClanRole role)
    {
        return role switch
        {
            ClanRole.Leader => ClanPermission.AddMember | ClanPermission.KickMember,
            _ => ClanPermission.None
        };
    }
}
