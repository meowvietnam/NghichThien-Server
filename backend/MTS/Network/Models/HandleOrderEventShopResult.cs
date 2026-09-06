namespace MTS.Network.Models;

public class HandleOrderEventShopResult : Result, IEventResultData
{
    public bool IsSuccess()
    {
        return Success;
    }
}