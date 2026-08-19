using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Models;

namespace MTS.Services;

public static class WelcomeRewardService
{
    public static async Task ClaimWelcomeReward(CharacterData charData, AppDbContext db)
    {
        var welcomeReward = await db.WelcomeRewards.FirstOrDefaultAsync(w => w.CharId == charData.Id);
        if (welcomeReward == null)
        {
            welcomeReward = new WelcomeReward
            {
                CharId = charData.Id,
                ClaimCount = 0,
                ClaimDate = DateTime.MinValue
            };
            await db.WelcomeRewards.AddAsync(welcomeReward);
        }
        if (Utils.Utils.IsToday(welcomeReward.ClaimDate))
        {
            return;
        }

        if (welcomeReward.ClaimCount >= 7)
        {
            return;
        }
        welcomeReward.ClaimCount++;
        welcomeReward.ClaimDate = DateTime.UtcNow;
        var mailManager = new MailManager(charData, charData.Client);
        await mailManager.SendMail(welcomeReward.CreateMail(), db);
    }
}