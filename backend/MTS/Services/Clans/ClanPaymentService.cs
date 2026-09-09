using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MTS.Data;
using MTS.Models;
using Network.Models.Clans;
using PayOS;
using PayOS.Models.V2.PaymentRequests;

namespace MTS.Services.Clans;

public record ClanCheckout(string Id, string Url);
public interface IClanPaymentGateway
{
    Task<ClanCheckout> CreateOrRecover(long orderId, long amount, DateTime expiresAt);
}
public sealed class ClanPaymentGateway(PayOSClient payOS, IConfiguration config) : IClanPaymentGateway
{
    public async Task<ClanCheckout> CreateOrRecover(long orderId, long amount, DateTime expiresAt)
    {
        var returnUrl = config["PayOS:ReturnUrl"];
        var cancelUrl = config["PayOS:CancelUrl"];
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var returnUri) || returnUri.Scheme != "https" ||
            !Uri.TryCreate(cancelUrl, UriKind.Absolute, out var cancelUri) || cancelUri.Scheme != "https")
            throw new ClanRuleException("PaymentNotConfigured");
        try
        {
            var link = await payOS.PaymentRequests.CreateAsync(new CreatePaymentLinkRequest {
                OrderCode = orderId, Amount = amount, Description = "Tao Tong Mon", ReturnUrl = returnUrl!, CancelUrl = cancelUrl!,
                ExpiredAt = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)).ToUnixTimeSeconds() });
            if (link.OrderCode != orderId || link.Amount != amount) throw new InvalidDataException("PayOS order mismatch");
            return new ClanCheckout(link.PaymentLinkId, link.CheckoutUrl);
        }
        catch
        {
            // Reuse the durable order code if the CREATE response or subsequent DB save was lost.
            var existing = await payOS.PaymentRequests.GetAsync(orderId);
            if (existing.OrderCode != orderId || existing.Amount != amount) throw new InvalidDataException("PayOS order mismatch");
            return new ClanCheckout(existing.Id, "https://pay.payos.vn/web/" + Uri.EscapeDataString(existing.Id));
        }
    }
}

public sealed class ClanPaymentService(IDbContextFactory<AppDbContext> factory, IOptions<ClanOptions> options,
    SnowflakeIdGenerator ids, IClanPaymentGateway gateway, ClanRealtimeService realtime)
{
    public async Task<ClanResponse> Prepare(int charId, Guid requestId)
    {
        if (charId <= 0 || requestId == Guid.Empty) throw new ClanRuleException("InvalidRequest");
        if (!options.Value.IsValid()) throw new ClanRuleException("InvalidClanConfiguration");
        using var strategyDb = factory.CreateDbContext();
        var payment = strategyDb.Database.CreateExecutionStrategy().Execute(() => {
            using var db = factory.CreateDbContext();
            using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
            var row = db.ClanCreationPayments.SingleOrDefault(x => x.CharId == charId && x.RequestId == requestId);
            if (row == null)
            {
                if (options.Value.CreationPayOsVnd <= 0) throw new ClanRuleException("CreationPriceNotConfigured");
                var now = DateTime.UtcNow;
                if (db.ClanCreationPayments.Count(x => x.CharId == charId && x.Status == ClanCreationPaymentStatus.Pending && x.ExpiresAt > now) >= 3)
                    throw new ClanRuleException("TooManyPendingPayments");
                row = new ClanCreationPayment { OrderId = ids.NextId(), CharId = charId, RequestId = requestId,
                    Amount = options.Value.CreationPayOsVnd, CreatedAt = now, ExpiresAt = now.AddHours(1) };
                db.ClanCreationPayments.Add(row);
                db.SaveChanges();
            }
            tx.Commit();
            return row;
        });
        if (payment.Status == ClanCreationPaymentStatus.Pending && payment.ExpiresAt <= DateTime.UtcNow)
            throw new ClanRuleException("PaymentLinkExpired");
        if (payment.Status == ClanCreationPaymentStatus.Pending && payment.CheckoutUrl.Length == 0)
        {
            if (payment.ExpiresAt <= DateTime.UtcNow) throw new ClanRuleException("PaymentLinkExpired");
            var link = await gateway.CreateOrRecover(payment.OrderId, payment.Amount, payment.ExpiresAt);
            using var db = factory.CreateDbContext();
            if (string.IsNullOrWhiteSpace(link.Id) || link.Id.Length > 128 || !Uri.TryCreate(link.Url, UriKind.Absolute, out var checkout) ||
                checkout.Scheme != "https" || link.Url.Length > 1024)
                throw new ClanRuleException("InvalidCheckout");
            // A webhook may arrive while the remote call is in flight. Never overwrite its bound ID.
            var updated = await db.ClanCreationPayments.Where(x => x.OrderId == payment.OrderId &&
                (x.PaymentLinkId == "" || x.PaymentLinkId == link.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.CheckoutUrl, link.Url).SetProperty(x => x.PaymentLinkId, link.Id));
            if (updated != 1) throw new ClanRuleException("InvalidPayment");
        }
        using var read = factory.CreateDbContext();
        payment = read.ClanCreationPayments.AsNoTracking().Single(x => x.OrderId == payment.OrderId);
        return new ClanResponse { RequestId = requestId, Success = true, Code = "Ok", Payments = [ToDto(payment)] };
    }

    // Only call after PayOS SDK signature verification. Never exposed as a client command.
    public bool AcceptVerified(long orderId, long amount, string currency, string code, string linkId, string reference)
    {
        using var strategyDb = factory.CreateDbContext();
        var charId = strategyDb.Database.CreateExecutionStrategy().Execute(() => {
            using var db = factory.CreateDbContext();
            using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
            var payment = db.ClanCreationPayments.SingleOrDefault(x => x.OrderId == orderId);
            if (payment == null) return 0;
            if (amount != payment.Amount || currency != "VND" || code != "00" || string.IsNullOrWhiteSpace(linkId) ||
                linkId.Length > 128 || (payment.PaymentLinkId.Length > 0 && payment.PaymentLinkId != linkId) ||
                string.IsNullOrWhiteSpace(reference) || reference.Length > 128)
                throw new ClanRuleException("InvalidPayment");
            if (payment.Status == ClanCreationPaymentStatus.Pending)
            {
                payment.Status = ClanCreationPaymentStatus.Paid;
                payment.PaymentLinkId = linkId;
                payment.PaymentReference = reference;
                payment.PaidAt = DateTime.UtcNow;
                db.SaveChanges();
            }
            // Delayed verified payment is honored even after link expiry. Duplicate callbacks succeed.
            tx.Commit();
            return payment.CharId;
        });
        if (charId == 0) return false;
        try { realtime.Publish([charId], new ClanChangedDto { EventId = Guid.NewGuid(), ClanId = 0 }); }
        catch (Exception ex) { Serilog.Log.Warning(ex, "Payment committed; client will recover on payment query for {CharId}", charId); }
        return true;
    }
    public static ClanPaymentDto ToDto(ClanCreationPayment x) => new() { OrderId = x.OrderId,
        Amount = x.Amount, CheckoutUrl = x.CheckoutUrl, Status = (int)x.Status, ExpiresAt = x.ExpiresAt, ClanId = x.ConsumedByClanId };
}
