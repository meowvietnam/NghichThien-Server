using MTS.Models;

namespace MTS.Network.Models;

public class HandleOrderRechargeEvent1 : IEventRequestData
{
    public Order Order { get; set; }
}