using MemoryPack;
using MTS.Models;
using MTS.Utils;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class BuyAndSellResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int TotalSellAmount { get; set; }
    public int TotalBuyAmount { get; set; }
    public int LinhThach { get; set; }
    public Shop ReloadShop { get; set; }
    public EMonPhai MonPhai { get; set; }
    public Modify<FurnaceData> Furnaces { get; set; } = new();
    public Modify<ItemRef> Items { get; set; } = new();

    public long GetTotalAmount()
    {
        return TotalSellAmount - TotalBuyAmount;
    }
}