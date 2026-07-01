using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class ReloadShopResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Shop Shop { get; set; }
}