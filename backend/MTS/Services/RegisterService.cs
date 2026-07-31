using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Services;

public static class RegisterService
{
    private static bool TryValidateRegister(string username, string password, out string message)
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

    private static async Task ValidateModel(RegisterModel model, AppDbContext db)
    {
        if (!TryValidateRegister(model.Username, model.Password, out var message))
        {
            throw new InvalidException(message);
        }

        if (await db.LocalAccounts.AnyAsync(x => x.Username == model.Username))
        {
            throw new InvalidException(Strings.UsernameAlreadyExists);
        }
    }

    private static LocalAccount CreateAccount(RegisterModel model)
    {
        var localAccount = new LocalAccount
        {
            Username = model.Username,
            PasswordHash = PasswordService.Hash(model.Password),
        };
        return localAccount;
    }

    public static async Task<RegisterResult> Register(RegisterModel model)
    {
        await using var db = await Program.CreateDbAsync();
        await ValidateModel(model, db);
        var account = CreateAccount(model);
        account.UserData = new();
        await db.LocalAccounts.AddAsync(account);
        await db.SaveChangesAsync();
        return new RegisterResult()
        {
            Success = true,
            AccountAlias = account.Username,
            Message = Strings.RegisterSuccess,
            UserData = new(account.UserData)
        };
    }

    public static async Task<RegisterResult> GuestRegister(GuestRegister request, UserData user)
    {
        if (user == null)
        {
            throw new InvalidException(Strings.UserDataInvalid);
        }
        await using var db = await Program.CreateDbAsync();
        var model = request.Model;
        await ValidateModel(model, db);
        var guestUsername = request.GuestUsername;
        var guestAccount = await db.GuestAccounts.FirstOrDefaultAsync(x => x.Username == guestUsername);
        if (guestAccount == null)
        {
            throw new InvalidException(Strings.GuestAccountNotFound);
        }

        var account = CreateAccount(model);
        account.UserId = user.Id;
        await db.LocalAccounts.AddAsync(account);
        db.GuestAccounts.Remove(guestAccount);
        await db.SaveChangesAsync();
        return new RegisterResult()
        {
            Success = true,
            AccountAlias = account.Username,
            Message = Strings.RegisterSuccess,
            UserData = new(user)
        };
    }
}
