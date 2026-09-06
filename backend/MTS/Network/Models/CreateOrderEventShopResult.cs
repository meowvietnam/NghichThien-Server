using MemoryPack;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class CreateOrderEventShopResult : Result, IEventResultData
{
    public string Link { get; set; }
    public bool IsSuccess()
    {
        return Success;
    }
}