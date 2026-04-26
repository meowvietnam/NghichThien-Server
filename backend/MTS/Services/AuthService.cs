using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Models;

namespace MTS.Services;

public class AuthService
{
    public async Task<UserData> CreateOrLogin(string username)
    {
        using var db = await Program.Db.CreateDbContextAsync();
        var account = await db.DeveloperAccounts
            .AsNoTracking()
            .Include(x => x.UserData)
            .FirstOrDefaultAsync(x => x.UserName == username);
        if (account != null)
        {
            var userId = account.UserId;
            var characterDatas =  await db.Characters
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Include(x => x.Progression)
                .Include(x => x.Appearance)
                .AsSplitQuery()
                .ToListAsync();
            account.UserData.CharacterDatas = characterDatas;
            return account.UserData;
        }
        account = new DeveloperAccount()
        {
            UserName = username,
            UserData = new UserData()
        };
        db.DeveloperAccounts.Add(account);
        await db.SaveChangesAsync();
        return account.UserData;
    }
}