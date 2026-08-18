using MemoryPack;
using MTS.Data;

namespace MTS.Models;
[MemoryPackable]
public partial class MailRewardMoneyVip : IMailReward
{
    public int MoneyVip { get; set; }
    public async Task ClaimReward(CharacterData characterData, AppDbContext db)
    {
        var newMoneyVip = characterData.MoneyVip + MoneyVip;
        characterData.SetMoneyVip(newMoneyVip);
    }
}