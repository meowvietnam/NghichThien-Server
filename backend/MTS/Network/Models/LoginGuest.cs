using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Localization;
using MTS.Models;

namespace MTS.Network.Models;
[MemoryPackable]
public partial class LoginGuest : ILogin
{
    public string Username { get; set; }
    public string DeviceId { get; set; }
    public SignInProvider GetSignInProvider()
    {
        return SignInProvider.Guest;
    }
    private async Task<UserData> CreateOrLogin(Client client)
    {
        await using var db = await Program.CreateDbAsync();
        var account = await db.GuestAccounts
            .AsNoTracking()
            .Include(x => x.UserData)
            .FirstOrDefaultAsync(x => x.Username == Username);
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

        var now = Utils.Utils.Now();
        var today = new DateTimeOffset(now.Date, Utils.Utils.Utc7).ToUniversalTime().DateTime;
        var tomorrow = new DateTimeOffset(now.Date.AddDays(1), Utils.Utils.Utc7).ToUniversalTime().DateTime;
        var count = await db.GuestAccounts.CountAsync(x => x.CreatedAt >= today && x.CreatedAt <= tomorrow);
        if (count > 10)
        {
            throw new InvalidException(Strings.GuestRegisterLimitReached);
        }
        account = new GuestAccount()
        {
            Username = Username,
            UserData = new UserData(),
            DeviceId = DeviceId,
            IPAddress = client.TcpClient.EndPoint.Address.MapToIPv4().ToString(),
        };
        var entityEntry = db.GuestAccounts.Add(account);
        await db.SaveChangesAsync();
        return entityEntry.Entity.UserData;
    }
    public async Task<LoginResult> Login(Client client)
    {
        var user = await CreateOrLogin(client);
        return new LoginResult()
        {
            Success = true,
            UserData = new (user),
            Message = Strings.LoginSuccess,
            AccountAlias = Strings.GuestAccountAlias
        };
    }
}
