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
    public CharacterStats GetReset(CharacterData charData)
    {
        var stats = charData.Stats.Clone();
        var level = charData.Progression.Level;
        var statsTemp = GameConfig.CharacterTemplate.Stats;
        var baseHp = statsTemp.SinhMenh;
        var estimateHp = baseHp;
        var infoLevels = GameConfig.LevelResource.infoLevels.Take(level - 1).ToArray();
        var ability = stats.Ability;
        var thanThuc = statsTemp.ThanThuc;
        var thoNguyen = 100;
        var donToc = statsTemp.TocDoBay;
        for (var i = 0; i < infoLevels.Length; i++)
        {
            var infoLevel = infoLevels[i];
            estimateHp += infoLevel.hpMaxBuff;
            if (ability.HasFlag(Ability.ThanMaHuyetMach))
            {
                estimateHp += (int)MathF.Ceiling(estimateHp * 0.2f);
            }

            if (i == 2 && ability.HasFlag(Ability.DiTocHuyetMach))
            {
                estimateHp *= 2;
            }

            thanThuc += infoLevel.thanThucBuff;
            thoNguyen += infoLevel.thoNguyenBuff;
            donToc += infoLevel.donTocBuff;
        }

        var hp = Math.Max(0, estimateHp);
        stats.SetSinhMenh(hp);
        stats.SetHp(hp);
        stats.SetThoNguyen(thoNguyen);
        stats.SetThanThuc(thanThuc);
        stats.SetTocDoBay(donToc);
        stats.ResetLinhCan();
        stats.ResetTuChat();
        stats.ResetNgoTinh();
        return stats;
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