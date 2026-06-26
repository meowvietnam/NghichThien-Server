using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Newtonsoft.Json;

namespace MTS.IAP;

public class GooglePlayIapConfig
{
    public string PackageName { get; set; }
    public string JsonPath { get; set; }
    public string ApplicationName { get; set; }
}
public static class GooglePlayIap
{
    private static GooglePlayIapConfig Config { get; set; }

    static GooglePlayIap()
    {
        Config = JsonConvert.DeserializeObject<GooglePlayIapConfig>(File.ReadAllText("Data/google-play-iap-config.json"));
    }
    private static readonly Lazy<AndroidPublisherService> Service =
        new(CreateService, LazyThreadSafetyMode.ExecutionAndPublication);

    private static AndroidPublisherService CreateService()
    {
        var jsonPath = Config.JsonPath;

        if (string.IsNullOrWhiteSpace(jsonPath))
        {
            throw new InvalidOperationException(
                "Thiếu biến GOOGLE_PLAY_SERVICE_ACCOUNT_JSON.");
        }

        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy Google service account JSON.", jsonPath);
        }

        var credential = GoogleCredential
            .FromFile(jsonPath)
            .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

        return new AndroidPublisherService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = Config.ApplicationName
            });
    }

    public static async Task<GooglePurchaseResult> VerifyAsync(
        string expectedProductId,
        string purchaseToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedProductId) ||
            string.IsNullOrWhiteSpace(purchaseToken))
        {
            return GooglePurchaseResult.Fail("Thiếu productId hoặc purchaseToken.");
        }

        try
        {
            var purchase = await Service.Value.Purchases.Productsv2
                .Getproductpurchasev2(Config.PackageName, purchaseToken)
                .ExecuteAsync(cancellationToken);

            var isPurchased = purchase.PurchaseStateContext?.PurchaseState
                              == "PURCHASED";

            if (!isPurchased)
            {
                return GooglePurchaseResult.Fail(
                    $"Purchase chưa hoàn tất: {purchase.PurchaseStateContext?.PurchaseState}");
            }

            var matchedProduct = purchase.ProductLineItem?
                .Any(x => x.ProductId == expectedProductId) == true;

            if (!matchedProduct)
            {
                return GooglePurchaseResult.Fail(
                    "Token không thuộc productId đang yêu cầu.");
            }
            return GooglePurchaseResult.Success(
                purchase.OrderId,
                purchase.PurchaseCompletionTimeDateTimeOffset,
                purchase.AcknowledgementState,
                purchase.ObfuscatedExternalAccountId);
        }
        catch (Google.GoogleApiException ex)
        {
            return GooglePurchaseResult.Fail(
                $"Google Play verify lỗi ({(int)ex.HttpStatusCode}): {ex.Message}");
        }
    }

    public static Task ConsumeAsync(
        string productId,
        string purchaseToken,
        CancellationToken cancellationToken = default)
    {
        return Service.Value.Purchases.Products
            .Consume(Config.PackageName, productId, purchaseToken)
            .ExecuteAsync(cancellationToken);
    }

    public static Task AcknowledgeAsync(
        string productId,
        string purchaseToken,
        CancellationToken cancellationToken = default)
    {
        return Service.Value.Purchases.Products
            .Acknowledge(
                new Google.Apis.AndroidPublisher.v3.Data.ProductPurchasesAcknowledgeRequest(),
                Config.PackageName,
                productId,
                purchaseToken)
            .ExecuteAsync(cancellationToken);
    }
}

public sealed record GooglePurchaseResult(
    bool IsValid,
    string? Error,
    string? OrderId,
    DateTimeOffset? PurchaseTime,
    string? AcknowledgementState,
    string? ObfuscatedAccountId)
{
    public static GooglePurchaseResult Success(
        string? orderId,
        DateTimeOffset? purchaseTime,
        string? acknowledgementState,
        string? obfuscatedAccountId)
        => new(
            true,
            null,
            orderId,
            purchaseTime,
            acknowledgementState,
            obfuscatedAccountId);

    public static GooglePurchaseResult Fail(string error)
        => new(false, error, null, null, null, null);
}