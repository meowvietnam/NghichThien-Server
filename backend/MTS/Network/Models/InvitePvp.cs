using MemoryPack;
using MTS.Network.ClientState;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class InvitePvp
{
    public int Inviter { get; set; }
    public int Defender { get; set; }
    public string InviterName { get; set; }
    public string DefenderName { get; set; }
    public DateTime Expire { get; set; }

    public bool IsExpire()
    {
        return DateTime.UtcNow > Expire;
    }
}