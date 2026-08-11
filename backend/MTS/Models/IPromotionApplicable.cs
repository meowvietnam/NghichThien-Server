using MemoryPack;
using MTS.IAP;
using MTS.Promotions;

namespace MTS.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MoneyVipPromotionApplicable))]
public partial interface IPromotionApplicable
{
    void Apply(IIapReward reward);
}