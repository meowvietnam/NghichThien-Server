namespace MTS.Utils;
using Microsoft.AspNetCore.Identity;

public static class PasswordService
{
    private static readonly PasswordHasher<object> Hasher = new();

    public static string Hash(string password)
    {
        return Hasher.HashPassword(null!, password);
    }

    public static bool Verify(string password, string passwordHash)
    {
        var result = Hasher.VerifyHashedPassword(
            null!,
            passwordHash,
            password
        );
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}