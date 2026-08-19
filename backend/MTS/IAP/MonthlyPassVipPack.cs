using MemoryPack;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.IAP;
[MemoryPackable]
public partial class MonthlyPassVipPack : IIapReward
{
    public int MoneyVipPerDay { get; set; }
    public void Edit(IapPurchase purchase)
    {
    }
    public List<IMailReward> GetMailRewards()
    {
        var items = MonthlyPassPack.RandomItem(6);
        var rewards =  MonthlyPassPack.GetMailRewards(items);
        rewards.Add(new MailMonthlyPass()
        {
            MoneyVipPerDay = MoneyVipPerDay,
            PassName = "Thẻ tháng VIP"
        });
        return rewards;
    }
}