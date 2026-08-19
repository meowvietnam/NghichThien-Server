using MTS.Exceptions;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Localization;
using MTS.Network.Models;
using MTS.IAP;
using MTS.Services;
using Serilog;

namespace MTS.Models;

public partial class CharacterData
{
    public async Task<IapResult> IapPurchase(IapModel request)
    {
        var verifiedPurchase = await VerifyIapPurchase(request);
        return await CompleteIapPurchase(verifiedPurchase);
    }

    public async Task<VerifiedIapPurchase> VerifyIapPurchase(IapModel request)
    {
        Log.Information(request.ToString());
        var store = request.Store;
        var productId = request.ProductId;
        if (!Enum.IsDefined(typeof(Store), store))
        {
            throw new InvalidException(Strings.IapStoreNotSupported);
        }

        await using var db = await Program.CreateDbAsync();
        var product = await db.IapProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Store == store
                                      && x.StoreProductId == productId
                                      && x.IsActive);
        if (product == null)
        {
            throw new InvalidException(Strings.IapProductNotFound);
        }

        var cancelToken = CancellationToken.None;
        var verify = await IapService.VerifyAsync(
            store,
            productId,
            request.ReceiptJson,
            product.ProductType,
            cancelToken);
        if (!verify.IsValid)
        {
            throw new InvalidException(verify.Error ?? Strings.IapStoreNotSupported);
        }

        var purchaseTokenHash = Utils.Utils.HashSHA256(verify.PurchaseToken);
        var hasPurchased = await db.IapPurchases
            .AnyAsync(x => x.Store == store
                           && x.PurchaseTokenHash == purchaseTokenHash,
                cancelToken);
        if (hasPurchased)
        {
            Log.Information(
                "IAP purchase already processed; Store={Store}, ProductId={ProductId}",
                store, productId);
            return new VerifiedIapPurchase(product, verify, purchaseTokenHash, true);
        }
        await CanBuyProduct(product, db);
        return new VerifiedIapPurchase(product, verify, purchaseTokenHash, false);
    }

    public async Task<Result> CanBuyProduct(Store store, string productId)
    {
        if (!Enum.IsDefined(typeof(Store), store))
        {
            throw new InvalidException(Strings.IapStoreNotSupported);
        }

        await using var db = await Program.CreateDbAsync();
        
        var product = await db.IapProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Store == store
                                      && x.StoreProductId == productId
                                      && x.IsActive);
        return await CanBuyProduct(product, db);
    }

    public async Task<Result> CanBuyProduct(Store store, int productId)
    {
        if (!Enum.IsDefined(typeof(Store), store))
        {
            throw new InvalidException(Strings.IapStoreNotSupported);
        }
        await using var db = await Program.CreateDbAsync();
        var product = await db.IapProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Store == store
                                      && x.Id == productId
                                      && x.IsActive);
        return await CanBuyProduct(product, db);
    }

    public async Task<Result> CanBuyProduct(IapProduct? product, AppDbContext db)
    {
        if (product == null)
        {
            throw new InvalidException(Strings.IapProductNotFound);
        }
        var purchases = await db.IapPurchases
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.CharId == Id)
            .ToListAsync();
        return await CanBuyProduct(product, db, purchases);
    }

    public async Task<Result> CanBuyProduct(IapProduct? product, AppDbContext db, List<IapPurchase> purchases)
    {
        if (product == null)
        {
            throw new InvalidException(Strings.IapProductNotFound);
        }

        var linkerId = product.LinkerId;
        if (linkerId != null && linkerId.Value != 0)
        {
            var linker = await db.ProductLinkers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == linkerId);
            if (linker != null)
            {
                if (!linker.CanBuyAgain)
                {
                    var hasPurchased = purchases
                        .Any(x => x.CharId == Id && x.Product.LinkerId == linkerId);
                    if (hasPurchased)
                    {
                        throw new InvalidException(Strings.IapPurchaseAlreadyProcessed);
                    }
                }
                else
                {
                    var now = DateTime.UtcNow;
                    var acceptDate = now - TimeSpan.FromDays(linker.NextDayCanBuy);
                    var hasPurchased = purchases
                        .Any(x => x.CharId == Id && x.Product.LinkerId == linkerId && x.PurchaseDate > acceptDate);
                    if (hasPurchased)
                    {
                        throw new InvalidException(Strings.IapPurchaseAlreadyProcessed);
                    }
                }
            }
        }

        return new Result()
        {
            Success = true,
        };
    }
    public async Task<IapResult> CompleteIapPurchase(VerifiedIapPurchase verifiedPurchase)
    {
        var product = verifiedPurchase.Product;
        Log.Information("IAP purchase requested; CharacterId={CharacterId}, Store={Store}, ProductId={ProductId}",
            Id, product.Store, product.StoreProductId);
        if (verifiedPurchase.IsAlreadyProcessed)
        {
            Log.Information(
                "IAP purchase already processed; CharacterId={CharacterId}, Store={Store}, ProductId={ProductId}",
                Id, product.Store, product.StoreProductId);
            return new IapResult()
            {
                Success = true,
                Reward = null,
                Store = product.Store,
                ProductId = product.StoreProductId
            };
        }

        var cancelToken = CancellationToken.None;
        await using var db = await Program.CreateDbAsync();
        var hasPurchased = await db.IapPurchases
            .AnyAsync(x => x.Store == product.Store
                           && x.PurchaseTokenHash == verifiedPurchase.PurchaseTokenHash,
                cancelToken);
        if (hasPurchased)
        {
            Log.Information(
                "IAP purchase became processed before completion; CharacterId={CharacterId}, Store={Store}, ProductId={ProductId}",
                Id, product.Store, product.StoreProductId);
            return new IapResult()
            {
                Success = true,
                Reward = null,
                Store = product.Store,
                ProductId = product.StoreProductId
            };
        }

        await CanBuyProduct(product, db);

        var purchase = new IapPurchase()
        {
            CharId = Id,
            ProductId = product.Id,
            Store = product.Store,
            PurchaseTokenHash = verifiedPurchase.PurchaseTokenHash,
            PurchaseDate = verifiedPurchase.Verify.PurchaseTime?.UtcDateTime ?? DateTime.UtcNow,
        };
        var reward = IIapReward.Clone(product.Reward);
        var promotions = await PromotionService.GetActivePromotions(product.Store, db);
        foreach (var promotion in promotions)
        {
            promotion.PromotionApplicable.Apply(reward);
        }
        reward.Edit(purchase);
        await db.IapPurchases.AddAsync(purchase, cancelToken);
        var mailRewards = reward.GetMailRewards();
        var mailManager = new MailManager(this, Client);
        await mailManager.SendMail(new Mail()
        {
            Title = "Nạp tiền thành công!",
            Content =
                $"Bạn vừa nạp thành công gói {product.Name}.\nCảm ơn bạn đã ủng hộ game! Chúc bạn chơi game vui vẻ và có những trải nghiệm thật tuyệt!",
            Rewards = mailRewards
        }, db);
        try
        {
            await IapService.FinalizeAsync(
                product.Store,
                product.StoreProductId,
                verifiedPurchase.Verify.PurchaseToken,
                product.ProductType,
                cancelToken);
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "IAP finalize failed after purchase was saved; CharacterId={CharacterId}, Store={Store}, ProductId={ProductId}, TransactionId={TransactionId}",
                Id,
                product.Store,
                product.StoreProductId,
                verifiedPurchase.Verify.TransactionId);
        }

        return new IapResult()
        {
            Success = true,
            Store = product.Store,
            ProductId = product.StoreProductId,
            Reward = reward,
        };
    }

    public async Task<List<OwnerIapProduct>> GetProducts(Store store, AppDbContext db)
    {
        var iapProducts = await db.IapProducts
            .Where(x => x.IsActive && x.Store == store)
            .ToArrayAsync();
        var purchases = await db.IapPurchases
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.CharId == Id)
            .ToListAsync();
        var ownerProducts = new List<OwnerIapProduct>(iapProducts.Length);
        foreach (var product in iapProducts)
        {
            var hasPurchased = await HasPurchased(product, db, purchases);
            var ownerIapProduct = new OwnerIapProduct()
            {
                HasPurchased = hasPurchased,
                Product = product,
            };
            ownerProducts.Add(ownerIapProduct);
        }
        return ownerProducts
            .OrderByDescending(x => x.Product.Priority)
            .ThenBy(x => x.Product.Name).ToList();
    }

    public async Task<bool> HasPurchased(IapProduct product, AppDbContext db, List<IapPurchase> purchases)
    {
        try
        {
            await CanBuyProduct(product, db, purchases);
            return false;
        }
        catch (InvalidException ex)
        {
            return true;
        }
    }
    public async Task<List<OwnerIapProduct>> GetProducts(Store store)
    {
        await using var db = await Program.CreateDbAsync();
        return await GetProducts(store, db);
    }

    public ExchangeMoneyVipResult ExchangeMoneyVipForLinhThach(int amount)
    {
        if (amount <= 0 || amount > MoneyVip)
        {
            throw new InvalidException(Strings.MoneyVipInvalid);
        }

        var linhThach = (int)Math.Min(int.MaxValue, Stats.LinhThach + amount * 100L);
        Stats.SetLinhThach(linhThach);
        var moneyVip = MoneyVip - amount;
        SetMoneyVip(moneyVip);
        return new ExchangeMoneyVipResult()
        {
            Success = true,
            LinhThach = linhThach,
            MoneyVip = moneyVip
        };
    }

    private bool IsOnline()
    {
        return Client != null;
    }

    public void AddSpendMoneyVipOnline(long moneyVip, SpendFor spendFor)
    {
        var spendingMoneyVip = new SpendingMoneyVip
        {
            CharId = Id,
            SpendFor = spendFor,
            MoneyVip = moneyVip,
        };
        addedSpendingMoneyVips.Add(spendingMoneyVip);
    }

    public async Task AddSpendMoneyVipOffline(long moneyVip, SpendFor spendFor, AppDbContext db)
    {
        var spendingMoneyVip = new SpendingMoneyVip
        {
            CharId = Id,
            SpendFor = spendFor,
            MoneyVip = moneyVip,
        };
        await db.SpendingMoneyVips.AddAsync(spendingMoneyVip);
    }

    public async Task AddSpendMoneyVipOffline(long moneyVip, SpendFor spendFor, DateTime time, AppDbContext db)
    {
        var spendingMoneyVip = new SpendingMoneyVip
        {
            CharId = Id,
            SpendFor = spendFor,
            MoneyVip = moneyVip,
            Time = time,
        };
        await db.SpendingMoneyVips.AddAsync(spendingMoneyVip);
    }

    public async Task<long> GetTongNapInRank()
    {
        await using var db = await Program.CreateDbAsync();
        var startUtc = RechargeRankService.GetTimeStart().UtcDateTime;
        var endUtc = RechargeRankService.GetTimeEnd().UtcDateTime;
        return await db.IapPurchases
            .Where(x => x.CharId == Id && x.PurchaseDate >= startUtc && x.PurchaseDate < endUtc)
            .SumAsync(x => x.MoneyVipSnapshot);
    }

    public async Task<long> GetTongTieuInRank()
    {
        await using var db = await Program.CreateDbAsync();
        var startUtc = MoneyVipSpendingRankService.GetTimeStart().UtcDateTime;
        var endUtc = MoneyVipSpendingRankService.GetTimeEnd().UtcDateTime;
        return await db.SpendingMoneyVips
            .Where(x => x.CharId == Id && x.Time >= startUtc && x.Time < endUtc)
            .SumAsync(x => x.MoneyVip);
    }
}

public sealed record VerifiedIapPurchase(
    IapProduct Product,
    IapVerifyResult Verify,
    byte[] PurchaseTokenHash,
    bool IsAlreadyProcessed);