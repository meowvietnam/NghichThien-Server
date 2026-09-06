using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class BuyItemEventShopResult : Result, IEventResultData
{
    public int NewMoneyVip { get; set; }
    public bool IsSuccess()
    {
        return Success;
    }
}