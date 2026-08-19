using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Models;

namespace MTS.Services;

public class DailyRewardMonthlyPassService
{
    public static async Task ClaimDailyRewardMonthlyPass(CharacterData charData, AppDbContext db)
    {
        var charId = charData.Id;
        var passes = await db.DailyRewardMonthlyPasses
            .Where(x => x.CharId == charId)
            .ToListAsync();
        foreach (var pass in passes)
        {
            if (pass.IsEnded())
            {
                continue;
            }
            if (pass.IsClaimedToday())
            {
                continue;
            }
            await pass.Claim(charData,db);
        }
    }
}