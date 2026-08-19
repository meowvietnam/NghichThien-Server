using MemoryPack;
using MTS.Data;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardMoney : IMailReward
{
    public int Money { get; set; }
    public async Task ClaimReward(CharacterData characterData, AppDbContext db)
    {
        var newMoney = characterData.Stats.LinhThach + Money;
        characterData.Stats.SetLinhThach(newMoney);
    }
}