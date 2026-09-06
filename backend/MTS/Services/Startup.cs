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
using MTS.Utils;
using Newtonsoft.Json;
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
                    return (long)(x.Product.Price * 0.7f);
                }

                return x.Product.Price;
            }

            return x.AmountSnapshot;
        });
        Log.Information("Total: {Total} VNĐ, Estimate: {Estimate} VNĐ", Utils.Utils.FormatCount(total),
            Utils.Utils.FormatCount(estimate));
    }

    private async Task ClaimMail(AppDbContext db)
    {
        var charData = await db.Characters.FirstAsync(x => x.Id == 1925);
        var @event = await db.EventManagers.Include(x => x.Events).FirstAsync(x => x.CharId == charData.Id);
        var rechargeEvent = @event.Events[0].GameEvent as RechargeEvent1;
        var mile = rechargeEvent.Milestones.First(x => x.Id == 48);
        var rewards = mile.GetRewards();
        var mail = new Mail()
        {
            CharId = charData.Id,
            Title = "Phần thưởng nạp tiền",
            Content = $"Chúc mừng! Bạn nhận được phần thưởng {mile.Name}.",
            Rewards = rewards
        };
        await db.Mails.AddAsync(mail);
        await db.SaveChangesAsync();
    }

    private async Task UpdateShop(AppDbContext db)
    {
        var chars = await db.Characters
            .ToListAsync();
        foreach (var charData in chars)
        {
            var shopManager = new ShopManager()
            {
                CharId = charData.Id,
                CharData = charData
            };
            shopManager.ReloadAll();
            var e = db.Attach(shopManager);
            e.Property(x => x.ShopDuocLieu).IsModified = true;
            e.Property(x => x.ShopVatLieu).IsModified = true;
            e.Property(x => x.ShopSkill).IsModified = true;
            e.Property(x => x.ShopSkillAll).IsModified = true;
        }

        var result = await db.SaveChangesAsync();
        Log.Information("Result: {result}", result);
    }

    private async Task UpdateQuest(AppDbContext db)
    {
        var charIds = await db.Characters.Select(x => x.Id).ToListAsync();
        var total = charIds.Count;
        var count = 0;
        foreach (var charId in charIds)
        {
            count++;
            if (count % 100 == 1)
            {
                Log.Information("Tiến độ: {count}/{total}", count, total);
            }

            var questManager = new QuestManager()
            {
                CharId = charId
            };
            questManager.InitDailyQuests();

            var dailyQuests = questManager.DailyQuests;
            var quests = dailyQuests.Values.SelectMany(x => x).ToList();
            foreach (var dailyQuest in quests)
            {
                await db.DailyQuests
                    .Where(x => x.CharId == charId && x.EDailyQuest == dailyQuest.EDailyQuest)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.QuestReward, dailyQuest.QuestReward));
            }
        }

        Log.Information("Completed");
    }

    private async Task AddEvents(AppDbContext db)
    {
        var charIds = await db.Characters.Select(x => x.Id).ToArrayAsync();
        foreach (var charId in charIds)
        {
            var events = EventService.CreateEvents();
            var eventManager = new EventManager()
            {
                CharId = charId,
                Events = events
            };
            await db.EventManagers.AddAsync(eventManager);
        }

        await db.SaveChangesAsync();
        Log.Information("Oke");
    }

    public async Task AddReward(AppDbContext db)
    {
        var ms = await db.MonsterKills.Include(x => x.Rewards).ToListAsync();
        foreach (var m in ms)
        {
            var reward1 = m.Rewards[0];
            if (reward1.Rewards.All(x => !(x is ItemEventReward)))
            {
                reward1.Rewards.Add(new ItemEventReward()
                {
                    Item = new ItemRef()
                    {
                        Id = 2027,
                        Type = ItemType.Herb,
                        Count = 3
                    }
                });
            }

            var reward2 = m.Rewards[1];
            if (reward2.Rewards.All(x => !(x is ItemEventReward)))
            {
                reward2.Rewards.Add(new ItemEventReward()
                {
                    Item = new ItemRef()
                    {
                        Id = 2027,
                        Type = ItemType.Herb,
                        Count = 6
                    }
                });
            }

            db.Entry(reward1).Property(x => x.Rewards).IsModified = true;
            db.Entry(reward2).Property(x => x.Rewards).IsModified = true;
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