using MemoryPack;

namespace MTS.Network.Models;

[MemoryPackable]
public partial class WatchAdsMoneyVipResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int MoneyVip { get; set; }
    public int Count { get; set; }
}