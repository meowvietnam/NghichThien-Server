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
                            | ClanPermission.ManageInheritance
                            | ClanPermission.ManageHon,
        [ClanRole.ViceLeader] = ClanPermission.AddMember
                                | ClanPermission.KickMember
                                | ClanPermission.AssignRole
                                | ClanPermission.AddShopItem
                                | ClanPermission.RemoveShopItem,
        [ClanRole.Elder] = ClanPermission.AddMember
                           | ClanPermission.KickMember
                           | ClanPermission.AssignRole
                           | ClanPermission.AddShopItem
                           | ClanPermission.RemoveShopItem,
        [ClanRole.Member] = ClanPermission.None
    };

    public static ClanPermission GetPermissions(ClanRole role)
    {
        return Permissions.GetValueOrDefault(role, ClanPermission.None);
    }
}
