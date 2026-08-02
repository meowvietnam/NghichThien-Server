using Newtonsoft.Json;

namespace MTS.IAP;

public static class IapService
{
    public static async Task<IapVerifyResult> VerifyAsync(
        Store store,
        string expectedProductId,
        string receiptJson,
        IapProductType productType,
        CancellationToken cancellationToken = default)
    {
        return store switch
        {
            Store.GooglePlay => await VerifyGooglePlayAsync(expectedProductId, receiptJson, cancellationToken),
            Store.AppStore => await AppStoreIap.VerifyAsync(expectedProductId, receiptJson, productType, cancellationToken),
            _ => IapVerifyResult.Fail($"IAP store is not supported: {store}")
        };
    }

    private static async Task<IapVerifyResult> VerifyGooglePlayAsync(
        string expectedProductId,
        string receiptJson,
        CancellationToken cancellationToken)
    {
        var purchaseToken = ExtractGooglePurchaseToken(receiptJson);
        var verify = await GooglePlayIap.VerifyAsync(expectedProductId, purchaseToken, cancellationToken);
        if (!verify.IsValid)
        {
            return IapVerifyResult.Fail(verify.Error ?? "Google Play purchase is invalid.");
        }

        return IapVerifyResult.Success(
            purchaseToken,
            verify.OrderId,
            verify.PurchaseTime);
    }

    public static Task FinalizeAsync(
        Store store,
        string productId,
        string purchaseToken,
        IapProductType productType,
        CancellationToken cancellationToken = default)
    {
        return store switch
        {
            Store.GooglePlay => FinalizeGooglePlayAsync(productId, purchaseToken, productType, cancellationToken),
            Store.AppStore => Task.CompletedTask,
            _ => Task.CompletedTask
        };
    }

    private static Task FinalizeGooglePlayAsync(
        string productId,
        string purchaseToken,
        IapProductType productType,
        CancellationToken cancellationToken)
    {
        return productType switch
        {
            IapProductType.Consumable => GooglePlayIap.ConsumeAsync(productId, purchaseToken, cancellationToken),
            IapProductType.NonConsumable => GooglePlayIap.AcknowledgeAsync(productId, purchaseToken, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    private static string ExtractGooglePurchaseToken(string receiptJson)
    {
        if (string.IsNullOrWhiteSpace(receiptJson))
        {
            return string.Empty;
        }

        try
        {
            var receipt = JsonConvert.DeserializeObject<GoogleReceipt>(receiptJson);
            if (receipt?.Payload == null)
            {
                return receiptJson;
            }

            var payload = JsonConvert.DeserializeObject<GooglePlayPayload>(receipt.Payload);
            var purchaseData = JsonConvert.DeserializeObject<GooglePurchaseData>(payload?.Json ?? string.Empty);
            return purchaseData?.PurchaseToken ?? payload?.Json ?? receiptJson;
        }
        catch
        {
            return receiptJson;
        }
    }

    private sealed class GoogleReceipt
    {
        [JsonProperty("Payload")] public string Payload { get; set; } = string.Empty;
    }

    private sealed class GooglePlayPayload
    {
        [JsonProperty("json")] public string Json { get; set; } = string.Empty;
        [JsonProperty("signature")] public string Signature { get; set; } = string.Empty;
        [JsonProperty("Store")] public string Store { get; set; } = string.Empty;
        [JsonProperty("TransactionID")] public string TransactionId { get; set; } = string.Empty;
    }

    private sealed class GooglePurchaseData
    {
        [JsonProperty("purchaseToken")] public string PurchaseToken { get; set; } = string.Empty;
    }
}

public sealed record IapVerifyResult(
    bool IsValid,
    string? Error,
    string PurchaseToken,
    string? TransactionId,
    DateTimeOffset? PurchaseTime)
{
    public static IapVerifyResult Success(
        string purchaseToken,
        string? transactionId,
        DateTimeOffset? purchaseTime)
    {
        return new IapVerifyResult(true, null, purchaseToken, transactionId, purchaseTime);
    }

    public static IapVerifyResult Fail(string error)
    {
        return new IapVerifyResult(false, error, string.Empty, null, null);
    }
}
