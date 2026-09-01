using MTS.Exceptions;
using MTS.Localization;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

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