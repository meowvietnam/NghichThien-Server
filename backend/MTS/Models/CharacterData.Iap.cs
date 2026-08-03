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

    public static async Task<VerifiedIapPurchase> VerifyIapPurchase(IapModel request)
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

        if (product.ProductType == IapProductType.NonConsumable)
        {
            hasPurchased = await db.IapPurchases.AnyAsync(
                x => x.Store == store && x.ProductId == product.Id,
                cancelToken);
            if (hasPurchased)
            {
                throw new InvalidException(Strings.IapPurchaseAlreadyProcessed);
            }
        }

        return new VerifiedIapPurchase(product, verify, purchaseTokenHash, false);
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

        if (product.ProductType == IapProductType.NonConsumable)
        {
            hasPurchased = await db.IapPurchases.AnyAsync(
                x => x.Store == product.Store && x.ProductId == product.Id,
                cancelToken);
            if (hasPurchased)
            {
                throw new InvalidException(Strings.IapPurchaseAlreadyProcessed);
            }
        }

        var purchase = new IapPurchase()
        {
            CharId = Id,
            ProductId = product.Id,
            Store = product.Store,
            PurchaseTokenHash = verifiedPurchase.PurchaseTokenHash,
            PurchaseDate = verifiedPurchase.Verify.PurchaseTime?.UtcDateTime ?? DateTime.UtcNow,
        };
        var reward = product.Reward;
        reward.Execute(this);
        reward.Edit(purchase);
        await db.IapPurchases.AddAsync(purchase, cancelToken);
        await db.SaveChangesAsync(cancelToken);
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
            Reward = product.Reward,
        };
    }

    public async Task<List<OwnerIapProduct>> GetProducts(Store store)
    {
        await using var db = await Program.CreateDbAsync();
        var iapProducts = await db.IapProducts
            .Where(x => x.IsActive && x.Store == store)
            .ToArrayAsync();
        var purchasedProductIds = await db.IapPurchases
            .Where(x => x.CharId == Id)
            .Select(x => x.ProductId)
            .ToHashSetAsync();
        var ownerProducts = iapProducts
            .Select(x =>
            {
                return new OwnerIapProduct()
                {
                    Product = x,
                    HasPurchased = purchasedProductIds.Contains(x.Id)
                };
            }).ToList();
        return ownerProducts;
    }

    public ExchangeMoneyVipResult ExchangeMoneyVipForLinhThach(int amount)
    {
        if (amount <= 0 || amount > MoneyVip)
        {
            throw new InvalidException(Strings.MoneyVipInvalid);
        }

        var linhThach = Stats.LinhThach + amount * 100;
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
