using MemoryPack;
using MTS.Data;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.IAP;
[MemoryPackable]
public partial class MoneyVipPack : IIapReward
{
    public int MoneyVip { get; set; }
    public int IconIndex { get; set; }
    public void Edit(IapPurchase purchase)
    {
        purchase.MoneyVipSnapshot += MoneyVip; 
    }
    public List<IMailReward> GetMailRewards()
    {
        return new List<IMailReward>()
        {
            new MailRewardMoneyVip()
            {
                MoneyVip = MoneyVip,
            }
        };
    }
}