using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Localization;

namespace MTS.Network.Models
{
    [MemoryPackable]
    public partial class AdminLoginWithName : ILogin
    {
        public string Name { get; set; }
        public string SecretKey { get; set; }
        public SignInProvider GetSignInProvider()
        {
            return SignInProvider.AdminName;
        }

        public async Task<LoginResult> Login(Client client)
        {
            if (SecretKey != ILogin.Key)
            {
                throw new InvalidException(Strings.AdminSecretKeyInvalid);
            }

            await using var db = await Program.CreateDbAsync();
            var charData = await db.Characters.AsNoTracking().FirstOrDefaultAsync(x => x.Name == Name);
            if (charData == null)
            {
                var name = Name.ToLower().Trim();
                var sameCharacters = await db.Characters
                    .AsNoTracking()
                    .Select(x => x.Name)
                    .Where(x => x.Trim().ToLower().Contains(name))
                    .ToListAsync();
                var message = $"Không tìm thấy người chơi này các người chơi liên quan:\n{string.Join("\n", sameCharacters.Select(x => $"<{x}>"))}";
                throw new InvalidException(message);
            }
            var user = await db.Users.AsNoTracking().FirstAsync(x => x.Id == charData.UserId);
            await user.LoadCharacterDatas(db);
            return new LoginResult()
            {
                Success = true,
                UserData = new (user),
                Message = $"Đăng nhập thành công với tài khoản {charData.Name}!",
                AccountAlias = charData.Name,
            };
        }
    }
}