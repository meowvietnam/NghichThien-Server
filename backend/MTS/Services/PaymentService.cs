using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.IAP;
using MTS.Localization;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using Serilog;

namespace MTS.Services;

public class PaymentService : BackgroundService
{
    private readonly PayOSClient payOS;
    private readonly SnowflakeIdGenerator idGenerator;
    private readonly IConfiguration config;
    public static PaymentService Instance { get; private set; }
    public PaymentService(PayOSClient payOs, SnowflakeIdGenerator idGenerator, IConfiguration config)
    {
        payOS = payOs;
        this.idGenerator = idGenerator;
        this.config = config;
    }

    public static async Task<(long Total, long Estimate)> GetRevenue()
    {
        await using var db = await Program.CreateDbAsync();
        var purchases = await db.IapPurchases
            .Include(x => x.Product)
            .ToListAsync();
        var total = purchases
            .Sum(x => x.AmountSnapshot);
        var estimate = purchases.Sum(x =>
        {
            if (x.Product != null)
            {
                if (x.Store == Store.GooglePlay || x.Store == Store.AppStore)
                {
                    return (long)(x.Product.Price * 0.7f);
                }

                return x.Product.Price;
            }

            return x.AmountSnapshot;
        });
        return (total, estimate);
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Instance = this;
        return Task.CompletedTask;
    }

    public async Task<CreatePaymentLinkResponse> CreateLink(long amount, TimeSpan expire)
    {
        var orderCode = idGenerator.NextId();
        var now = DateTime.UtcNow;
        var expireTime = now.Add(expire);
        var expireUnixTime = new DateTimeOffset(expireTime).ToUnixTimeSeconds();
        var cancelUrl = config["PayOS:CancelUrl"];
        var returnUrl = config["PayOS:ReturnUrl"];
        if (string.IsNullOrWhiteSpace(cancelUrl) || string.IsNullOrWhiteSpace(returnUrl))
        {
            throw new InvalidException(Strings.PaymentConfigInvalid);
        }
        var paymentRequest = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amount,
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl,
            ExpiredAt = expireUnixTime,
        };
        var paymentLink = await payOS.PaymentRequests.CreateAsync(paymentRequest);
        return paymentLink;
    }
}