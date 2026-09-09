namespace MTS.Models;

public static class ClanRolePermissions
{
    public static ClanPermission GetPermissions(ClanRole role)
    {
        return ClanPermission.None;
    }
}