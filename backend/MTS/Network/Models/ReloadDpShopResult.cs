using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class ReloadDpShopResult : Result
{
    public DongPhuShop Shop { get; set; }
}