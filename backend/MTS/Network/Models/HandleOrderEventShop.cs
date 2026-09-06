using MTS.Models;

namespace MTS.Network.Models;

public class HandleOrderEventShop : IEventRequestData
{
    public Order Order { get; set; }
}