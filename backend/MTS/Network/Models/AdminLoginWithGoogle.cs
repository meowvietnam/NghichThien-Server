using MTS.Exceptions;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Localization;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class AdminLoginWithGoogle : ILogin
    {
        public string Email { get; set; }
        public string SecretKey { get; set; }

        public SignInProvider GetSignInProvider()
        {
            return SignInProvider.AdminGoogle;
        }

        public async Task<LoginResult> Login(Client client)
        {
            if (SecretKey != ILogin.Key)
            {
                throw new InvalidException(Strings.AdminSecretKeyInvalid);
            }

            await using var db = await Program.CreateDbAsync();
            var account = await db.GoogleAccounts
                .AsNoTracking()
                .Include(x => x.UserData)
                .FirstOrDefaultAsync(x => x.Email == Email);
            if (account == null)
            {
                throw new InvalidException(Strings.AdminGoogleAccountNotFound);
            }
            await account.UserData.LoadCharacterDatas(db);
            var userData = account.UserData;
            return new LoginResult()
            {
                Success = true,
                UserData = new (userData),
                Message = $"Đăng nhập thành công với tài khoản Google {Email}!",
                AccountAlias = Email,
            };
        }
    }
}
