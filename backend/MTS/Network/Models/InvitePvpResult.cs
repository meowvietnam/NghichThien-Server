using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class InvitePvpResult : Result 
{
    public InvitePvp InvitePvp { get; set; }
}