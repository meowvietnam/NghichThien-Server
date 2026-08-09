using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.IAP;
using MTS.Localization;
using MTS.Models;
using MTS.Network.Models;
using MTS.Services;
using Newtonsoft.Json;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Serilog;

namespace MTS.Controllers;

public record CreateLinkRequest(int ProductId);

[Route("/pay")]
public class PaymentController : Controller
{
    private readonly PayOSClient payOS;
    private readonly SnowflakeIdGenerator idGenerator;
    private readonly AppDbContext db;

    public PaymentController(PayOSClient payOS, SnowflakeIdGenerator idGenerator, AppDbContext db)
    {
        this.payOS = payOS;
        this.idGenerator = idGenerator;
        this.db = db;
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
                .FirstOrDefaultAsync(x => x.Store == Store.Web && x.IsActive && x.Id == productId);
            if (product == null)
            {
                throw new InvalidException(Strings.IapProductNotFound);
            }

            var actualProductType = product.ActualProductType;
            if (actualProductType == IapProductType.NonConsumable)
            {
                var hasPurchased =
                    await db.IapPurchases
                        .AnyAsync(x => x.CharId == charId && x.Product.ActualProductType == actualProductType);
                if (hasPurchased)
                {
                    throw new InvalidException(Strings.IapPurchaseAlreadyProcessed);
                }
            }

            var orderCode = idGenerator.NextId();
            var paymentRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = product.Price,
                Description = "thanh toan",
                CancelUrl = "https://nghichthien.online/payment",
                ReturnUrl = "https://nghichthien.online/payment"
            };
            var paymentLink = await payOS.PaymentRequests.CreateAsync(paymentRequest);
            var oder = new Order()
            {
                CharId = charId,
                ProductId = productId,
                OrderId = orderCode,
                Link = paymentLink.CheckoutUrl,
                Amount = product.Price,
            };
            var entry = await db.Orders.AddAsync(oder);
            await db.SaveChangesAsync();
            return Success(entry.Entity);
        }
        catch (Exception ex)
        {
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
            ownerProducts.RemoveAll(x =>
                x.Product.ActualProductType == IapProductType.NonConsumable && x.HasPurchased);
            var products = ownerProducts.Select(x => x.Product).ToList();
            return Success(products);
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
            var data = await payOS.Webhooks.VerifyAsync(webhook);
            var order = await db.Orders
                .Include(o => o.CharData)
                .Include(o => o.Product)
                .Include(o => o.Purchase)
                .FirstOrDefaultAsync(x => x.OrderId == data.OrderCode);
            if (order == null)
            {
                throw new InvalidException(Strings.PaymentOrderNotFound);
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
            order.Product.Reward.Edit(purchase);
            order.Purchase = purchase;
            var rewards = order.Product.Reward.GetMailRewards();
            var mailManager = new MailManager(order.CharData, null);
            await mailManager.SendMail(new Mail()
            {
                Title = "Nạp tiền thành công!",
                Content =
                    $"Bạn vừa nạp thành công gói {order.Product.Name}.\nCảm ơn bạn đã ủng hộ game! Chúc bạn chơi game vui vẻ và có những trải nghiệm thật tuyệt!",
                Rewards = rewards
            }, db);
            return Success(order);
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
            return Fail(ex.Message);
        }
    }
}