using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class LoginGuest : ILogin
{
    public string Username { get; set; }
    public SignInProvider GetSignInProvider()
    {
        return SignInProvider.Guest;
    }
    private async Task<UserData> CreateOrLogin(string username)
    {
        await using var db = await Program.CreateDbAsync();
        var account = await db.GuestAccounts
            .AsNoTracking()
            .Include(x => x.UserData)
            .FirstOrDefaultAsync(x => x.Username == username);
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
        account = new GuestAccount()
        {
            Username = username,
            UserData = new UserData()
        };
        var entityEntry = db.GuestAccounts.Add(account);
        await db.SaveChangesAsync();
        return entityEntry.Entity.UserData;
    }
    public async Task<LoginResult> Login()
    {
        var user = await CreateOrLogin(Username);
        return new LoginResult()
        {
            Success = true,
            UserData = new (user),
            Message = "Đăng nhập thành công!",
            AccountAlias = "Khách"
        };
    }
}