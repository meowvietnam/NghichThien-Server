using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class MoneyVipMonsterRankReward : IMonsterRankReward
{
    public int MoneyVip { get; set; }
    public void Claim(CharacterData charData)
    {
        var newMoneyVip = charData.MoneyVip + MoneyVip;
        charData.SetMoneyVip(newMoneyVip);
    }
}