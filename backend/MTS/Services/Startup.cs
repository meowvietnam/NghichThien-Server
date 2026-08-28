using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.BaseSkill;
using MTS.Game.Gacha;
using MTS.Game.Resources;
using MTS.IAP;
using MTS.Models;
using MTS.ND;
using MTS.Network.Models;
using Serilog;
using NgoDao = MTS.ND.NgoDao;

namespace MTS.Services;

public class Startup : BackgroundService
{
    private readonly ILogger<Startup> logger;

    public Startup(ILogger<Startup> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var db = await Program.CreateDbAsync();
        var purchases = await db.IapPurchases
            .Include(x => x.Product)
            .ToListAsync(stoppingToken);
        var total = purchases
            .Sum(x => x.AmountSnapshot);
        var estimate = purchases.Sum(x =>
        {
            if (x.Product != null)
            {
                if (x.Store == Store.GooglePlay || x.Store == Store.AppStore)
                {
                    return (int)(x.Product.Price * 0.7f);
                }

                return x.Product.Price;
            }

            return x.AmountSnapshot;
        });
        Log.Information("Total: {Total} VNĐ, Estimate: {Estimate} VNĐ", Utils.Utils.FormatCount(total),
            Utils.Utils.FormatCount(estimate));
    }

    private async Task Edit(AppDbContext db)
    {
        var purchases = await db.IapPurchases.Include(x => x.Product).ToListAsync();
        foreach (var p in purchases)
        {
            p.AmountSnapshot = p.Product.Price;
        }

        await db.SaveChangesAsync();
        Log.Information("Okeeee");
    }
    public async Task AddReward(AppDbContext db)
    {
        var ms = await db.MonsterKills.Include(x => x.Rewards).ToListAsync();
        foreach (var m in ms)
        {
            var reward = m.Rewards[2];
            if (reward.Rewards.All(x => !(x is LinhDanMonsterRankReward)))
            {
                reward.Rewards.Add(new LinhDanMonsterRankReward()
                {
                    Id = 4670,
                    Count = 1
                });
                db.Entry(reward).Property(x => x.Rewards).IsModified = true;
            }
        }

        await db.SaveChangesAsync();
        Log.Information("Oke");
    }
    
    private async Task<CharacterData> FindCharacter(string name, AppDbContext db)
    {
        return await db.Characters.FirstAsync(x => x.Name == name);
    }

    private async Task<IapProduct> FindProduct(int id, AppDbContext db)
    {
        var product = await db.IapProducts.FirstAsync(x => x.Id == id);
        return product;
    }

    public async Task Buy(CharacterData charData, IapProduct product, AppDbContext db)
    {
        var purchase = new IapPurchase()
        {
            CharId = charData.Id,
            ProductId = product.Id,
            Store = product.Store,
            PurchaseTokenHash = Array.Empty<byte>(),
        };
        var productReward = IIapReward.Clone(product.Reward);
        var promotions = await PromotionService.GetActivePromotions(Store.Web, db);
        foreach (var promotion in promotions)
        {
            promotion.PromotionApplicable.Apply(productReward);
        }

        productReward.Edit(purchase);
        await db.IapPurchases.AddAsync(purchase);
        var rewards = productReward.GetMailRewards();
        var mailManager = new MailManager(charData, null);
        await mailManager.SendMail(new Mail()
        {
            Title = "Nạp tiền thành công!",
            Content =
                $"Bạn vừa nạp thành công gói {product.Name}.\nCảm ơn bạn đã ủng hộ game! Chúc bạn chơi game vui vẻ và có những trải nghiệm thật tuyệt!",
            Rewards = rewards
        }, db);
    }
}