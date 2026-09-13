namespace MTS.Models;

public static class ClanRolePermissions
{
    public static readonly Dictionary<ClanRole, ClanPermission> Permissions = new()
    {
        [ClanRole.Leader] = ClanPermission.AddMember
                            | ClanPermission.KickMember
                            | ClanPermission.AddShopItem
                            | ClanPermission.RemoveShopItem
                            | ClanPermission.AssignRole
                            | ClanPermission.ManageInheritance,
        [ClanRole.ViceLeader] = ClanPermission.AddMember
                                | ClanPermission.KickMember
                                | ClanPermission.AssignRole,
        [ClanRole.Elder] = ClanPermission.AddMember
                           | ClanPermission.KickMember
                           | ClanPermission.AssignRole,
        [ClanRole.Member] = ClanPermission.None
    };

    public static ClanPermission GetPermissions(ClanRole role)
    {
        return Permissions.GetValueOrDefault(role, ClanPermission.None);
    }
}
