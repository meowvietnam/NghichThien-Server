using MemoryPack;
using MTS.Data;

namespace MTS.Models;
[MemoryPackable]
public partial class MailMonthlyPass : IMailReward
{
    public string PassName { get; set; }
    public int MoneyVipPerDay { get; set; }
    public async Task ClaimReward(CharacterData characterData, AppDbContext db)
    {
        var charId = characterData.Id;
        var rewards = new List<IMailReward>
        {
            new MailRewardMoneyVip()
            {
                MoneyVip = MoneyVipPerDay,
            }
        };
        var dailyReward = new DailyRewardMonthlyPass
        {
            CharId = charId,
            Rewards = rewards,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };
        await db.DailyRewardMonthlyPasses.AddAsync(dailyReward);
        await db.SaveChangesAsync();
    }
}