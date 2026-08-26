using MemoryPack;
using MTS.Models;

namespace MTS.Game.DP;
[MemoryPackable]
public partial class DongPhuRewardMoneyVip : IDongPhuReward
{
    public int MoneyVip { get; set; }
    public void Claim(CharacterData charData)
    {
        var newMoneyVip = charData.MoneyVip + MoneyVip;
        charData.SetMoneyVip(newMoneyVip);
    }

    public string GetText()
    {
        return $"{Utils.Utils.FormatCount(MoneyVip)} Tiên ngọc";
    }
}