using Google.Apis.Auth;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Localization;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class LoginWithGoogle : ILogin
{
    public string IdToken { get; set; }
    
    public SignInProvider GetSignInProvider()
    {
        return SignInProvider.Google;
    }

    public async Task<LoginResult> Login()
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(IdToken);
        }
        catch
        {
            throw new Exception(Strings.GoogleLoginInvalid);
        }
        var email = payload.Email;
        var name = payload.GivenName;
        var userData = await CreateOrLogin(email);
        return new LoginResult()
        {
            Success = true,
            UserData = new (userData),
            Message = $"Đăng nhập thành công với tài khoản Google {name}!",
            WelcomeText = $"Chào mừng đạo hữu {name} bước vào Tiên Giới"
        };
    }

    private async Task<UserData> CreateOrLogin(string email)
    {
        await using var db = await Program.CreateDbAsync();
        var account = await db.GoogleAccounts
            .AsNoTracking()
            .Include(x => x.UserData)
            .FirstOrDefaultAsync(x => x.Email == email);
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
        account = new GoogleAccount()
        {
            Email = email,
            UserData = new UserData()
        };
        var entityEntry = db.GoogleAccounts.Add(account);
        await db.SaveChangesAsync();
        return entityEntry.Entity.UserData;
    }
}
