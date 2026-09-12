namespace MTS.Models;
[Flags]
public enum ClanPermission : uint
{
    None = 0,
    AddMember = 1 << 0,
    KickMember = 1 << 1,
    AddShopItem = 1 << 2,
    RemoveShopItem = 1 << 3,
    AssignRole = 1 << 4,
}
