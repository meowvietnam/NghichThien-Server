using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.IAP;
using MTS.Localization;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using MTS.Services;
using MTS.Utils;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Serilog;
using static MTS.Utils.Utils;

namespace MTS.Controllers;

public record CreateLinkRequest(int ProductId);

[Route("/pay")]
public class PaymentController : Controller
{
    private readonly PayOSClient payOS;
    private readonly SnowflakeIdGenerator idGenerator;
    private readonly AppDbContext db;
    private readonly IConfiguration config;

    public PaymentController(PayOSClient payOS, SnowflakeIdGenerator idGenerator, AppDbContext db,
        IConfiguration config)
    {
        this.payOS = payOS;
        this.idGenerator = idGenerator;
        this.db = db;
        this.config = config;
    }

    [HttpPost("create-link")]
    [Authorize]
    public async Task<IActionResult> CreateLink([FromBody] CreateLinkRequest request)
    {
        try
        {
            if (!TryGetId(out var charId))
            {
                throw new InvalidException(Strings.NotLoggedIn);
            }

            var charData = await db.Characters
                .FirstOrDefaultAsync(c => c.Id == charId);
            if (charData == null)
            {
                throw new InvalidException(Strings.CharacterNotFound);
            }

            var productId = request.ProductId;
            var product = await db.IapProducts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Store == Store.Web && x.IsActive && x.Id == productId);
            await charData.CanBuyProduct(product, db);
            var orderCode = idGenerator.NextId();
            var now = DateTime.UtcNow;
            var expireTime = now.AddHours(1);
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
                Amount = product.Price,
                CancelUrl = cancelUrl,
                ReturnUrl = returnUrl,
                ExpiredAt = expireUnixTime,
            };
            var paymentLink = await payOS.PaymentRequests.CreateAsync(paymentRequest);

            var oder = new Order()
            {
                CharId = charId,
                ProductId = productId,
                OrderId = orderCode,
                Link = paymentLink.CheckoutUrl,
                Amount = product.Price,
                ExpireAt = expireTime
            };
            var entry = await db.Orders.AddAsync(oder);
            await db.SaveChangesAsync();
            return Success(entry.Entity);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            return Fail(ex.Message);
        }
    }

    [HttpGet("pack")]
    [Authorize]
    public async Task<IActionResult> GetPack()
    {
        try
        {
            if (!TryGetId(out var charId))
            {
                throw new InvalidException(Strings.NotLoggedIn);
            }

            var charData = await db.Characters.FirstOrDefaultAsync(c => c.Id == charId);
            if (charData == null)
            {
                throw new InvalidException(Strings.CharacterNotFound);
            }

            var ownerProducts = await charData.GetProducts(Store.Web, db);
            ownerProducts.RemoveAll(x => x.HasPurchased);
            var products = ownerProducts.Select(x => x.Product).ToList();
            var promotions = await PromotionService.GetActivePromotions(Store.Web, db);
            return Success(new
            {
                Products = products,
                Promotions = promotions,
            });
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] Webhook webhook)
    {
        try
        {
            var data = await VerifyWebhook(webhook);
            var order = await db.Orders
                .Include(o => o.CharData)
                .Include(o => o.Product)
                .Include(o => o.Purchase)
                .FirstOrDefaultAsync(x => x.OrderId == data.OrderCode);
            if (order == null)
            {
                throw new InvalidException(Strings.PaymentOrderNotFound);
            }

            if (order.ExpireAt < DateTime.UtcNow)
            {
                throw new InvalidException(Strings.PaymentOrderExpired);
            }

            if (order.Amount != data.Amount)
            {
                throw new InvalidException(Strings.PaymentAmountInvalid);
            }

            if (order.Status != OrderStatus.Processing)
            {
                throw new InvalidException(Strings.PaymentOrderStatusInvalid);
            }

            order.Status = OrderStatus.Complete;
            var purchase = new IapPurchase()
            {
                CharId = order.CharId,
                ProductId = order.ProductId,
                Store = Store.Web,
                PurchaseTokenHash = Array.Empty<byte>(),
            };
            order.Purchase = purchase;
            var productReward = IIapReward.Clone(order.Product.Reward);
            var promotions = await PromotionService.GetActivePromotions(Store.Web, db);
            foreach (var promotion in promotions)
            {
                promotion.PromotionApplicable.Apply(productReward);
            }

            var charData = TcpServer.Instance.GetCharsOnline()
                .FirstOrDefault(c => c.Id == order.CharId);
            charData ??= order.CharData;
            productReward.Edit(purchase);
            var rewards = productReward.GetMailRewards();
            var mailManager = new MailManager(charData, charData.Client);
            await mailManager.SendMail(new Mail()
            {
                Title = "Nạp tiền thành công!",
                Content =
                    $"Bạn vừa nạp thành công gói {order.Product.Name}.\nCảm ơn bạn đã ủng hộ game! Chúc bạn chơi game vui vẻ và có những trải nghiệm thật tuyệt!",
                Rewards = rewards
            }, db);
            Log.Information("{Name} vừa mua gói {ProductName}: {Price} VNĐ", charData.Name, order.Product.Name,
                FormatCount(order.Amount));
            return Success(order);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            return Fail(ex.Message);
        }
    }

    private async Task<WebhookData> VerifyWebhook(Webhook webhook)
    {
        try
        {
            return await payOS.Webhooks.VerifyAsync(webhook);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Invalid PayOS webhook verification data");
            throw new InvalidException(Strings.PaymentWebhookInvalid);
        }
    }
}