using MTS.Exceptions;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.ItemProvider;
using MTS.Game.LuyenDan;
using MTS.Game.Resources;
using MTS.Game.Social;
using MTS.Localization;
using MTS.Network.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.IAP;
using MTS.Game.Combat;
using MTS.Network;
using MTS.Services;
using Serilog;

namespace MTS.Models;

public partial class CharacterData
{
    public async Task<IapResult> IapPurchase(IapModel request)
    {
        var store = request.Store;
        var productId = request.ProductId;
        var purchaseToken = request.PurchaseToken;
        Log.Information("IAP purchase requested; CharacterId={CharacterId}, Store={Store}, ProductId={ProductId}",
            Id, store, productId);
        var purchaseTokenHash = Utils.Utils.HashSHA256(purchaseToken);
        await using var db = await Program.CreateDbAsync();
        var product = await db.IapProducts
            .AsNoTracking()
            .FirstAsync(x => x.Store == store
                             && x.StoreProductId == productId
                             && x.IsActive);
        var hasPurchased = await db.IapPurchases
            .AnyAsync(x => x.Store == store
                           && x.ProductId == product.Id
                           && x.PurchaseTokenHash == purchaseTokenHash);
        if (hasPurchased)
        {
            Log.Information("IAP purchase already processed; CharacterId={CharacterId}, Store={Store}, ProductId={ProductId}",
                Id, store, productId);
            return new IapResult()
            {
                Success = true,
                Reward = null,
                Store = store,
                ProductId = productId
            };
        }

        if (product.ActualProductType == IapProductType.NonConsumable)
        {
            hasPurchased = await db.IapPurchases.AnyAsync(x => x.Store == store && x.ProductId == product.Id);
            if (hasPurchased)
            {
                throw new InvalidException(Strings.IapPurchaseAlreadyProcessed);
            }
        }

        var cancelToken = CancellationToken.None;
        if (store == Store.GooglePlay)
        {
            var verify = await GooglePlayIap.VerifyAsync(productId, purchaseToken, cancelToken);
            if (!verify.IsValid)
            {
                throw new InvalidException(verify.Error);
            }

            var productType = product.ProductType;
            if (productType == IapProductType.Consumable)
            {
                await GooglePlayIap.ConsumeAsync(productId, purchaseToken, cancelToken);
            }
            else if (productType == IapProductType.NonConsumable)
            {
                await GooglePlayIap.AcknowledgeAsync(productId, purchaseToken, cancelToken);
            }

            var purchase = new IapPurchase()
            {
                CharId = Id,
                ProductId = product.Id,
                Store = store,
                PurchaseTokenHash = purchaseTokenHash,
            };
            var reward = product.Reward;
            reward.Execute(this);
            reward.Edit(purchase);
            await db.IapPurchases.AddAsync(purchase, cancelToken);
            await db.SaveChangesAsync(cancelToken);
            return new IapResult()
            {
                Success = true,
                Store = store,
                ProductId = productId,
                Reward = product.Reward,
            };
        }

        throw new InvalidException($"{Strings.IapStoreNotSupported}: {store}");
    }

    public async Task<List<OwnerIapProduct>> GetProducts()
    {
        await using var db = await Program.CreateDbAsync();
        var iapProducts = await db.IapProducts.Where(x => x.IsActive).ToArrayAsync();
        var ownerProducts = iapProducts
            .Select(x =>
            {
                var hasPurchased = db.IapPurchases
                    .Any(p => p.CharId == Id && p.Store == x.Store && p.ProductId == x.Id);
                return new OwnerIapProduct()
                {
                    Product = x,
                    HasPurchased = hasPurchased
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
