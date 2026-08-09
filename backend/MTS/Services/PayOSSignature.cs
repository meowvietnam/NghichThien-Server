using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PayOS.Models.Webhooks;

namespace MTS.Services;

public static class PayOSSignature
{
    public static bool IsValidWebhook(
        Webhook webhook,
        string checksumKey)
    {
        var data = JsonSerializer.SerializeToElement(
            webhook.Data,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        var transactionStr = string.Join("&",
            data.EnumerateObject()
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .Select(x =>
                {
                    var value = x.Value.ValueKind switch
                    {
                        JsonValueKind.String => x.Value.GetString() ?? "",
                        JsonValueKind.Null => "",
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => x.Value.GetRawText()
                    };

                    return $"{x.Name}={value}";
                }));
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(checksumKey));

        var hash = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(transactionStr));

        var signature = Convert
            .ToHexString(hash)
            .ToLowerInvariant();

        return string.Equals(
            signature,
            webhook.Signature,
            StringComparison.OrdinalIgnoreCase);
    }
}