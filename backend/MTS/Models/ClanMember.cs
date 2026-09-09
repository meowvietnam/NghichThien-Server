namespace MTS.Models;
public class ClanMember
{
    public int ClanId { get; set; }
    public int CharId { get; set; }
    public ClanRole Role { get; set; }


    public bool HasPermission(ClanPermission permission)
    {
        var permissions = ClanRolePermissions.GetPermissions(Role);
        return permissions.HasFlag(permission);
    }
}