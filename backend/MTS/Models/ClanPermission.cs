namespace MTS.Models;
[Flags]
public enum ClanPermission : uint
{
    None = 0,
    AddMember = 1 << 0,
    KickMember = 1 << 1,
}
