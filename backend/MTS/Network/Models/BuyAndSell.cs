using MemoryPack;
using MTS.Models;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class BuyAndSell
{
    public List<ItemRef> BuyItems { get; set; }
    public List<ItemRef> SellItems { get; set; }
    public EShopType ShopType { get; set; }
    public EMonPhai MonPhai { get; set; }
}