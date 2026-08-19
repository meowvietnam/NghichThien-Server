using MemoryPack;
using MTS.Data;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.IAP;
[MemoryPackable]
[MemoryPackUnion(0, typeof(StartPack))]
[MemoryPackUnion(1, typeof(MoneyVipPack))]
[MemoryPackUnion(2, typeof(MonthlyPassPack))]
[MemoryPackUnion(3, typeof(MonthlyPassVipPack))]
public partial interface IIapReward
{
    void Edit(IapPurchase purchase);
    List<IMailReward> GetMailRewards();
    public static IIapReward Clone(IIapReward reward)
    {
        var bytes = MemoryPackSerializer.Serialize(reward);
        return MemoryPackSerializer.Deserialize<IIapReward>(bytes);
    }
}