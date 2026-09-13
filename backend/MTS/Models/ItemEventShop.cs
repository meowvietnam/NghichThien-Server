using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class ItemEventShop
{
    public ushort Id { get; set; }
    public bool HasBuy { get; set; }
    public IEventReward Reward { get; set; }
    public int Amount { get; set; }
    public int MoneyVip { get; set; }
    public int BonusMoneyVip { get; set; }

    public List<IMailReward> GetRewards()
    {
        return Reward.GetMailReward();
    }
}
