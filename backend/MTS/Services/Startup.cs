using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
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
    
    private async Task CreateMonterKills(AppDbContext db)
    {
        var characters = await db.Characters.ToListAsync();
        var monsterKills = characters.Select(x =>
        {
            var monsterKill = new MonsterKill()
            {
                CharData = x,
                CharId = x.Id,
            };
            monsterKill.Init();
            return monsterKill;
        }).ToArray();
        await db.MonsterKills.AddRangeAsync(monsterKills);
        await db.SaveChangesAsync();
        Log.Information("Monter Kills: {MonterKills}", monsterKills.Length);
    }

    private async Task ResetChars(AppDbContext db)
    {
        var mails = await db.Mails.Where(x => x.Title == "Xin lỗi và đền bù reset chỉ số trùng sinh").ToListAsync();
        var charIds = mails.Select(x => x.CharId).ToHashSet();
        var chars = await db.Characters
            .Include(x => x.Stats)
            .Include(x => x.Progression)
            .Where(x => charIds.Contains(x.Id))
            .ToListAsync();
        foreach (var charData in chars)
        {
            var currentStats = charData.Stats;
            var resetStats = GetReset(charData);
            var level = charData.Progression.Level;
            var name = charData.Name;
            var hp = currentStats.SinhMenh;
            var thoNguyen = currentStats.ThoNguyen;
            var thanThuc = currentStats.ThanThuc;
            var dontoc = currentStats.TocDoBay;

            var newHp = resetStats.SinhMenh;
            var newThoNguyen = resetStats.ThoNguyen;
            var newDontoc = currentStats.TocDoBay;
            var newThanThuc = resetStats.ThanThuc;
            Log.Information(
                "Reset {Name}, Level: {Level} Hp: {Hp}-{NewHp}, ThoNguyen: {ThoNguyen}-{NewThoNguyen}, DonToc: {DonToc}-{NewDontoc}, ThanThuc: {ThanThuc}-{NewThanThuc}",
                name, level, hp, newHp, thoNguyen, newThoNguyen, dontoc, newDontoc, thanThuc, newThanThuc);
            charData.Stats = resetStats;
            // var mail1 = new Mail()
            // {
            //     CharId = charData.Id,
            //     Title = "Xin lỗi và đền bù reset chỉ số trùng sinh",
            //     Content = "Nhằm đảm bảo cân bằng game, admin xin phép reset chỉ số anh em đã trùng sinh, lần reset này bạn nên trùng sinh lại 1 lần nữa để tối ưu chỉ số nhất.",
            //     Rewards = new List<IMailReward>()
            //     {
            //         new MailRewardMoneyVip()
            //         {
            //             MoneyVip = 10_000,
            //         }
            //     }
            // };
            // await db.Mails.AddAsync(mail1);
            // if (count > 0)
            // {
            //     var total = 0;
            //     for (int i = 0; i < count; i++)
            //     {
            //         var index = Math.Clamp(i, 0, prices.Length - 1);
            //         var price = prices[index];
            //         total += price;
            //     }
            //
            //     var mail2 = new Mail()
            //     {
            //         CharId = charData.Id,
            //         Title = "Hoàn tiên ngọc đã trùng sinh",
            //         Content = "Hoàn tiên ngọc đã trùng sinh",
            //         Rewards = new List<IMailReward>()
            //         {
            //             new MailRewardMoneyVip()
            //             {
            //                 MoneyVip = total,
            //             }
            //         }
            //     };
            //     await db.Mails.AddAsync(mail2);
            //     charData.SetTrungSinhCount(0);
            // }
            // await charData.RefundLinhDan(db);
        }

        await db.SaveChangesAsync();
        Log.Information("Oke");
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