using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class InvitePvpResponse
{
    public bool Accept { get; set; }
}