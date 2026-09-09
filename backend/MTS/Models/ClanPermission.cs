namespace MTS.Models;

[Flags]
public enum ClanPermission : uint
{
    None = 0,
    ReviewApplications = 1,
    KickMembers = 2,
    EditSettings = 4,
    ChangeRoles = 8,
    TransferLeadership = 16,
    Dissolve = 32
}