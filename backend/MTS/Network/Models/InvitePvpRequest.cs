using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class InvitePvpRequest
{
    public int Defender { get; set; }
}