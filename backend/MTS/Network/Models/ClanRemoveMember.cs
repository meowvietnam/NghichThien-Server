using MemoryPack;

namespace MTS.Network.Models;

public enum ClanRemoveMemberReason
{
    Kick = 1,
    Leave = 2
}

[MemoryPackable]
public partial class ClanRemoveMember
{
    public int ClanId { get; set; }
    public int CharId { get; set; }
    public ClanRemoveMemberReason Reason { get; set; }
}
