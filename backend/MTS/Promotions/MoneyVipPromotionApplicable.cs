using MemoryPack;
using MTS.IAP;
using MTS.Models;
using MTS.Utils;

namespace MTS.Promotions;
[MemoryPackable]
public partial class MoneyVipPromotionApplicable : IPromotionApplicable
{
    public float Percent { get; set; }

    public void Apply(IIapReward reward)
    {
        if (reward is MoneyVipPack moneyVipPack)
        {
            var moneyVip = moneyVipPack.MoneyVip;
            moneyVipPack.MoneyVip = Mathf.RoundToInt(moneyVip * (1 + Percent / 100f));
        }
        else if (reward is StartPack startPack)
        {
            var moneyVip = startPack.MoneyVip;
            startPack.MoneyVip = Mathf.RoundToInt(moneyVip * (1 + Percent / 100f));
        }
    }
}