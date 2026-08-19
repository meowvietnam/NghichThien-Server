using MemoryPack;
using MTS.Data;
using MTS.Models;
using MTS.Network.Models;

namespace MTS.IAP;

[MemoryPackable]
public partial class StartPack : IIapReward
{
    public int MoneyVip { get; set; }
    public int Money { get; set; }
    public ushort CongPhapId { get; set; }

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
            },
            new MailRewardMoney()
            {
                Money = Money,
            },
            new MailRewardItem()
            {
                Item = new ItemRef()
                {
                    Type = ItemType.CongPhap,
                    Id = CongPhapId,
                    Count = 1,
                }
            },
            new MailSetAds()
            {
                NoAds = true
            },
            new MailSetInfinityEnergy()
            {
                IsInfinityEnergy = true
            }
        };
    }
}