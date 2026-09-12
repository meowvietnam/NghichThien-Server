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
[MemoryPackable]
public partial class RequestJoinClanRequest
{
    public int ClanId { get; set; }
}

[MemoryPackable]
public partial class RequestJoinClanResult : Result
{
}

[MemoryPackable]
public partial class ClanJoinRequestAdded
{
    public ClanJoinRequest Request { get; set; }
}

[MemoryPackable]
public partial class ApproveClanJoinRequestRequest
{
    public int CharId { get; set; }
}

[MemoryPackable]
public partial class ApproveClanJoinRequestResult : Result
{
    public ClanMember Member { get; set; }
}

[MemoryPackable]
public partial class RejectClanJoinRequestRequest
{
    public int CharId { get; set; }
}

[MemoryPackable]
public partial class RejectClanJoinRequestResult : Result
{
    public int CharId { get; set; }
}

[MemoryPackable]
public partial class ClanJoinRequestRemoved
{
    public int ClanId { get; set; }
    public int CharId { get; set; }
}