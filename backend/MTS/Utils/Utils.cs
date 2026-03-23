using System.Security.Cryptography;

namespace MTS.Utils;

public class Utils
{
    public static string GenerateSecureString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[length];

        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[length];
        rng.GetBytes(buffer);
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[buffer[i] % chars.Length];
        }
        return new string(result);
    }
}