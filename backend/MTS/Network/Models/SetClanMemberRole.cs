using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class SetClanMemberRoleRequest
{
    public int CharId { get; set; }
    public ClanRole Role { get; set; }
}

[MemoryPackable]
public partial class SetClanMemberRoleResult : Result
{
    public ClanMemberRoleChanged? Change { get; set; }
}

[MemoryPackable]
public partial class ClanMemberRoleChanged
{
    public int ClanId { get; set; }
    public int CharId { get; set; }
    public ClanRole Role { get; set; }
}
