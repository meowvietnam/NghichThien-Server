using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardMoney : IMailReward
{
    public int Money { get; set; }
    public void ClaimReward(CharacterData characterData)
    {
        var newMoney = characterData.Stats.LinhThach + Money;
        characterData.Stats.SetLinhThach(newMoney);
    }
}