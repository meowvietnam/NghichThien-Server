using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Models;
namespace MTS.Game.Auth;

public class AuthService
{
    public UserData CreateOrLogin(string username)
    {
        using var db = AppDbFactory.Create();
        var account = db.DeveloperAccounts
            .Include(x => x.UserData)
            .FirstOrDefault(x => x.UserName == username);
        if (account != null)
        {
            db.Entry(account.UserData)
                .Collection(x => x.CharacterDatas)
                .Query()
                .Include(x => x.Stats)
                .Load();
            return account.UserData;
        }
        account = new DeveloperAccount()
        {
            UserName = username,
            UserData = new UserData()
        };
        db.DeveloperAccounts.Add(account);
        db.SaveChanges();
        return account.UserData;
    }
}