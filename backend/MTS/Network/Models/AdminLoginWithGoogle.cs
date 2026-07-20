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
        [MemoryPackIgnore] private const string Key = "qnn22n2us7g2bbna-aql11.e22111m1m";

        public SignInProvider GetSignInProvider()
        {
            return SignInProvider.Google;
        }

        public async Task<LoginResult> Login()
        {
            if (SecretKey != Key)
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

            var userId = account.UserId;
            var characterDatas = await db.Characters
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Include(x => x.Progression)
                .Include(x => x.Appearance)
                .AsSplitQuery()
                .ToListAsync();
            account.UserData.CharacterDatas = characterDatas;
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
