using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial struct ReloadShopRequest
{
    public EShopType ShopType { get; set; }
    public EMonPhai MonPhai { get; set; }
}