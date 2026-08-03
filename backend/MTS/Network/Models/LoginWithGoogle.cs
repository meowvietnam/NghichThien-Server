using MTS.Exceptions;
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

    public async Task<LoginResult> Login(Client client)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(IdToken);
        }
        catch
        {
            throw new InvalidException(Strings.GoogleLoginInvalid);
        }
        var email = payload.Email;
        var name = payload.Name;
        var userData = await CreateOrLogin(email);
        return new LoginResult()
        {
            Success = true,
            UserData = new (userData),
            Message = $"Đăng nhập thành công với tài khoản Google {name}!",
            AccountAlias = name
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
            await account.UserData.LoadCharacterDatas(db);
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
