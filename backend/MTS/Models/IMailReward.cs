using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MailRewardItem))]
[MemoryPackUnion(1, typeof(MailRewardFurnace))]
[MemoryPackUnion(2, typeof(MailRewardMoney))]
[MemoryPackUnion(3, typeof(MailRewardMoneyVip))]
public partial interface IMailReward
{
    public void ClaimReward(CharacterData characterData);
}