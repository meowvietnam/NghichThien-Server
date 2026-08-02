using Newtonsoft.Json;

namespace MTS.IAP;

public class AppStoreIapConfig
{
    public string SharedSecret { get; set; } = string.Empty;
    public string BundleId { get; set; } = string.Empty;
    public bool ExcludeOldTransactions { get; set; } = true;
}

public static class AppStoreIap
{
    private const int SuccessStatus = 0;
    private const int SandboxReceiptSentToProduction = 21007;
    private const int ProductionReceiptSentToSandbox = 21008;
    private const string ProductionVerifyUrl = "https://buy.itunes.apple.com/verifyReceipt";
    private const string SandboxVerifyUrl = "https://sandbox.itunes.apple.com/verifyReceipt";

    private static readonly HttpClient HttpClient = new();
    private static readonly AppStoreIapConfig Config = LoadConfig();

    public static async Task<IapVerifyResult> VerifyAsync(
        string expectedProductId,
        string receiptJson,
        IapProductType productType,
        CancellationToken cancellationToken = default)
    {
        var receiptData = ExtractReceiptData(receiptJson);
        if (string.IsNullOrWhiteSpace(expectedProductId) || string.IsNullOrWhiteSpace(receiptData))
        {
            return IapVerifyResult.Fail("Thiếu productId hoặc receipt.");
        }

        var response = await VerifyReceiptAsync(ProductionVerifyUrl, receiptData, cancellationToken);
        if (response.Status == SandboxReceiptSentToProduction)
        {
            response = await VerifyReceiptAsync(SandboxVerifyUrl, receiptData, cancellationToken);
        }
        else if (response.Status == ProductionReceiptSentToSandbox)
        {
            response = await VerifyReceiptAsync(ProductionVerifyUrl, receiptData, cancellationToken);
        }

        if (response.Status != SuccessStatus)
        {
            return IapVerifyResult.Fail($"Apple App Store verify lỗi: {response.Status}");
        }

        if (!string.IsNullOrWhiteSpace(Config.BundleId)
            && !string.Equals(response.Receipt?.BundleId, Config.BundleId, StringComparison.Ordinal))
        {
            return IapVerifyResult.Fail("Receipt không thuộc ứng dụng đang cấu hình.");
        }

        var transactions = (response.LatestReceiptInfo ?? [])
            .Concat(response.Receipt?.InApp ?? [])
            .Where(x => x.ProductId == expectedProductId
                        && string.IsNullOrWhiteSpace(x.CancellationDateMs))
            .ToArray();

        var transaction = transactions
                              .Where(x => x.ProductId == expectedProductId)
                              .OrderByDescending(x => ParseAppleDate(x.PurchaseDateMs))
                              .FirstOrDefault();

        if (transaction == null)
        {
            return IapVerifyResult.Fail("Receipt không thuộc productId đang yêu cầu.");
        }

        var transactionId = !string.IsNullOrWhiteSpace(transaction.TransactionId)
            ? transaction.TransactionId
            : transaction.OriginalTransactionId;
        if (string.IsNullOrWhiteSpace(transactionId))
        {
            return IapVerifyResult.Fail("Receipt không có transactionId hợp lệ.");
        }

        if (productType == IapProductType.Subscription)
        {
            var expiresDate = ParseAppleDate(transaction.ExpiresDateMs);
            if (expiresDate == null || expiresDate <= DateTimeOffset.UtcNow)
            {
                return IapVerifyResult.Fail("Gói subscription đã hết hạn.");
            }
        }

        return IapVerifyResult.Success(transactionId, transactionId, ParseAppleDate(transaction.PurchaseDateMs));
    }

    private static AppStoreIapConfig LoadConfig()
    {
        var configPath = "Data/app-store-iap-config.json";
        if (!File.Exists(configPath))
        {
            return new AppStoreIapConfig();
        }

        return JsonConvert.DeserializeObject<AppStoreIapConfig>(File.ReadAllText(configPath))
               ?? new AppStoreIapConfig();
    }

    private static async Task<AppStoreVerifyReceiptResponse> VerifyReceiptAsync(
        string url,
        string receiptData,
        CancellationToken cancellationToken)
    {
        var request = new AppStoreVerifyReceiptRequest
        {
            ReceiptData = receiptData,
            Password = string.IsNullOrWhiteSpace(Config.SharedSecret) ? null : Config.SharedSecret,
            ExcludeOldTransactions = Config.ExcludeOldTransactions
        };
        var json = JsonConvert.SerializeObject(request);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await HttpClient.PostAsync(url, content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonConvert.DeserializeObject<AppStoreVerifyReceiptResponse>(responseJson)
               ?? new AppStoreVerifyReceiptResponse { Status = -1 };
    }

    private static string ExtractReceiptData(string receiptJson)
    {
        if (string.IsNullOrWhiteSpace(receiptJson))
        {
            return string.Empty;
        }

        try
        {
            var receipt = JsonConvert.DeserializeObject<AppStoreReceiptPayload>(receiptJson);
            return receipt?.ReceiptData
                   ?? receipt?.Payload
                   ?? receiptJson;
        }
        catch
        {
            return receiptJson;
        }
    }

    private static DateTimeOffset? ParseAppleDate(string? milliseconds)
    {
        if (!long.TryParse(milliseconds, out var value))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(value);
    }

    private sealed class AppStoreVerifyReceiptRequest
    {
        [JsonProperty("receipt-data")] public string ReceiptData { get; set; } = string.Empty;
        [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
        public string? Password { get; set; }
        [JsonProperty("exclude-old-transactions")] public bool ExcludeOldTransactions { get; set; }
    }

    private sealed class AppStoreVerifyReceiptResponse
    {
        [JsonProperty("status")] public int Status { get; set; }
        [JsonProperty("receipt")] public AppStoreReceipt? Receipt { get; set; }
        [JsonProperty("latest_receipt_info")] public AppStoreTransaction[] LatestReceiptInfo { get; set; } = [];
    }

    private sealed class AppStoreReceipt
    {
        [JsonProperty("bundle_id")] public string BundleId { get; set; } = string.Empty;
        [JsonProperty("in_app")] public AppStoreTransaction[] InApp { get; set; } = [];
    }

    private sealed class AppStoreTransaction
    {
        [JsonProperty("product_id")] public string ProductId { get; set; } = string.Empty;
        [JsonProperty("transaction_id")] public string TransactionId { get; set; } = string.Empty;
        [JsonProperty("original_transaction_id")] public string OriginalTransactionId { get; set; } = string.Empty;
        [JsonProperty("purchase_date_ms")] public string PurchaseDateMs { get; set; } = string.Empty;
        [JsonProperty("expires_date_ms")] public string ExpiresDateMs { get; set; } = string.Empty;
        [JsonProperty("cancellation_date_ms")] public string CancellationDateMs { get; set; } = string.Empty;
    }

    private sealed class AppStoreReceiptPayload
    {
        [JsonProperty("receipt-data")] public string? ReceiptData { get; set; }
        [JsonProperty("Payload")] public string? Payload { get; set; }
    }
}
