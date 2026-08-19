using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.IAP;
using MTS.Models;
using MTS.Network.Models;
using Serilog;

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
            .Sum(x => x.Product.Price);
        var estimate = purchases.Sum(x =>
        {
            if (x.Store == Store.GooglePlay || x.Store == Store.AppStore)
            {
                return (int)(x.Product.Price * 0.7f);
            }

            return x.Product.Price;
        });
        Log.Information("Total: {Total} VNĐ, Estimate: {Estimate} VNĐ", Utils.Utils.FormatCount(total),
            Utils.Utils.FormatCount(estimate));

    }

    private async Task TopSpending(AppDbContext db)
    {
        var service = new MoneyVipSpendingRankService(null);
        var token = CancellationToken.None;
        var start = GameConfig.GameReleaseDate;
        var end = start + MoneyVipSpendingRankService.RankInterval;
        var tops = await service.GetTopsAsync(start, end, token);
        for (var i = 0; i < tops.Count; i++)
        {
            Log.Information("Top: {top}, Name: {name}, MoneyVip: {moneyVip}", i+1, tops[i].CharacterWrapper.Character.Name, Utils.Utils.FormatCount(tops[i].TotalSpent));
        }
        // await service.GrantRewards(tops, token);
        Log.Information("Xong");
    }
    private async Task SendMails(AppDbContext db)
    {
        var charDatas = await db.Characters.ToArrayAsync();
        for (var i = 0; i < charDatas.Length; i++)
        {
            var charData = charDatas[i];
            var mail = new Mail()
            {
                CharId = charData.Id,
                Title = "Nạp Web Nhận Thêm 30% Tiên Ngọc!",
                Content =
                    "Nạp tiền tự động tại https://play.nghichthien.online và nhận ngay +30% Tiên Ngọc. Nạp nhanh – cộng tự động – ưu đãi hấp dẫn.",
                Rewards = new List<IMailReward>()
                {
                    new MailRewardMoneyVip()
                    {
                        MoneyVip = 1
                    }
                }
            };
            await db.Mails.AddAsync(mail);
            Log.Information("[{i}/{j}] Send mail to {Name}", i + 1, charDatas.Length, charData.Name);
        }

        await db.SaveChangesAsync();
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