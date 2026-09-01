namespace MTS.Network.Models;

public class HandleOrderRechargeEvent1Result : Result, IEventResultData
{
    public bool IsSuccess()
    {
        return Success;
    }
}