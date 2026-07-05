using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardMoneyVip : IMailReward
{
    public int MoneyVip { get; set; }
    public void ClaimReward(CharacterData characterData)
    {
        var newMoneyVip = characterData.MoneyVip + MoneyVip;
        characterData.SetMoneyVip(newMoneyVip);
    }
}