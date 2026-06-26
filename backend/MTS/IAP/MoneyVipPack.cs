using MemoryPack;
using MTS.Models;

namespace MTS.IAP;
[MemoryPackable]
public partial class MoneyVipPack : IIapReward
{
    public int MoneyVip { get; set; }
    public int IconIndex { get; set; }
    public void Execute(CharacterData charData)
    {
        charData.SetMoneyVip(charData.MoneyVip + MoneyVip);
    }

    public void Refund(CharacterData charData)
    {
        charData.SetMoneyVip(charData.MoneyVip - MoneyVip);
    }
}