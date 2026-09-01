using MemoryPack;
using MTS.Models;

namespace MTS.Game.DP;
[MemoryPackable]
public partial class DongPhuRewardMoney : IDongPhuReward
{
    public int Money { get; set; }
    public DongPhuRewardMoney(int money)
    {
        Money = money;
    }
    public void Claim(CharacterData charData)
    {
        var newMoney = charData.Stats.LinhThach + Money;
        charData.Stats.SetLinhThach(newMoney);
    }
    public string GetText()
    {
        return $"{Utils.Utils.FormatCount(Money)} Linh thạch";
    }
}