using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Localization;
using MTS.Utils;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class LoginWithUsernamePassword  : ILogin
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public SignInProvider GetSignInProvider()
        {
            return SignInProvider.UsernamePassword;
        }
        private static bool TryValidateLogin(string username, string password, out string message)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                message = Strings.UsernameRequired;
                return false;
            }
            if (username.Length < 4)
            {
                message = Strings.UsernameTooShort;
                return false;
            }
            if (username.Length > 32)
            {
                message = Strings.UsernameTooLong;
                return false;
            }
            foreach (var character in username)
            {
                if (!char.IsWhiteSpace(character))
                {
                    continue;
                }

                message = Strings.UsernameContainsWhitespace;
                return false;
            }
            if (string.IsNullOrEmpty(password))
            {
                message = Strings.PasswordRequired;
                return false;
            }
            if (password.Length < 6)
            {
                message = Strings.PasswordTooShort;
                return false;
            }
            if (password.Length > 64)
            {
                message = Strings.PasswordTooLong;
                return false;
            }
            message = string.Empty;
            return true;
        }
        public async Task<LoginResult> Login(Client client)
        {
            if (!TryValidateLogin(Username, Password, out var message))
            {
                throw new InvalidException(message);
            }
            await using var db = await Program.CreateDbAsync();
            var account = await db.LocalAccounts.FirstOrDefaultAsync(x => x.Username == Username);
            if (account == null)
            {
                throw new InvalidException(Strings.UsernameOrPasswordInvalid);
            }

            if (!PasswordService.Verify(Password, account.PasswordHash))
            {
                throw new InvalidException(Strings.UsernameOrPasswordInvalid);
            }
            var userId = account.UserId;
            var user = await db.Users.FirstAsync(x => x.Id == userId);
            var characterDatas = await db.Characters
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Include(x => x.Progression)
                .Include(x => x.Appearance)
                .AsSplitQuery()
                .ToListAsync();
            user.CharacterDatas = characterDatas;
            return new LoginResult()
            {
                Success = true,
                UserData = new (user),
                Message = string.Format(Strings.LoginWithAccountSuccess, Username),
                AccountAlias = Username,
            };
        }
    }
}
