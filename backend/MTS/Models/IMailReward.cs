using MemoryPack;
using MTS.Data;

namespace MTS.Models;

[MemoryPackable]
[MemoryPackUnion(0, typeof(MailRewardItem))]
[MemoryPackUnion(1, typeof(MailRewardFurnace))]
[MemoryPackUnion(2, typeof(MailRewardMoney))]
[MemoryPackUnion(3, typeof(MailRewardMoneyVip))]
[MemoryPackUnion(4, typeof(MailSetAds))]
[MemoryPackUnion(5, typeof(MailSetInfinityEnergy))]
[MemoryPackUnion(6, typeof(MailMonthlyPass))]
public partial interface IMailReward
{
    public Task ClaimReward(CharacterData characterData, AppDbContext db);
}