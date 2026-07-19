using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial struct LoadShopRequest
    {
        public EShopType ShopType { get; set; }
        public EMonPhai MonPhai { get; set; }
    }
}