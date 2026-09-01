using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class MoneyVipEventReward : IEventReward
{
    public int MoneyVip { get; set; }
    public void Claim(CharacterData charData)
    {
        var newMoneyVip = charData.MoneyVip + MoneyVip;
        charData.SetMoneyVip(newMoneyVip);
    }

    public List<IMailReward> GetMailReward()
    {
        return new List<IMailReward>()
        {
            new MailRewardMoneyVip()
            {
                MoneyVip = MoneyVip
            }
        };
    }
}