using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class AddClanMemberRequest
{
    public int CharId { get; set; }
    public ClanRole Role { get; set; } = ClanRole.Member;
}

[MemoryPackable]
public partial class AddClanMemberResult : Result
{
    public ClanMember? Member { get; set; }
}
