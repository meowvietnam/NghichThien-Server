using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class ClanAddMember
{
    public ClanMember Member { get; set; } = null!;
}
