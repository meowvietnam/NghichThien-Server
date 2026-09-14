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
        await DeleteAllEvents(db);
        await AddEvents(db);
        // await SendOpenServerRechargeMoneyVipBonus(db, new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc));
        // var result = await AppDbContextMerge.Merge("Server=localhost;Port=3306;Database=MTS_Backup_09;User=root;Password=Linhdz123@;",
        //     "Server=localhost;Port=3306;Database=MTS_Backup_08;User=root;Password=Linhdz123@;",
        //     new AppDbContextMergeOptions
        //     {
        //         DryRun = false,
        //         SourceSuffix = "B"
        //     });
        // Log.Information("Fail rows: {rows}", result.FailedRows);
        // Log.Information("Skip rows: {rows}", result.SkippedRows);
        // Log.Information("Insert rows: {rows}", result.InsertedRows);
        // Log.Information("Source rows: {rows}", result.SourceRows);
        // Log.Information("---------------------------------");
        // foreach (var r in result.Tables)
        // {
        //     Log.Information("Table name: {name}", r.TableName);
        //     Log.Information("Fail rows: {rows}", r.FailedRows);
        //     Log.Information("Skip rows: {rows}", r.SkippedRows);
        //     Log.Information("Insert rows: {rows}", r.InsertedRows);
        //     Log.Information("Source rows: {rows}", r.SourceRows);
        //     Log.Information("Erros: {e}", string.Join(", ", r.Errors));
        //     Log.Information("--------------------------------");
        // }
    }

    private async Task DeleteAllEvents(AppDbContext db)
    {
        await db.Events.ExecuteDeleteAsync();
        await db.SaveChangesAsync();
        Log.Information("Oke");
    }

    private async Task SendOpenServerRechargeMoneyVipBonus(
        AppDbContext db,
        DateTime fromUtc,
        DateTime? toUtc = null,
        int multiplier = 2)
    {
        if (multiplier <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be positive.");
        }

        var endUtc = toUtc ?? DateTime.UtcNow;
        const string bonusTitle = "Phần thưởng nạp open server";
        var purchaseMoneyVipByChar = await db.IapPurchases
            .AsNoTracking()
            .Where(x => x.CharId != null
                        && x.PurchaseDate >= fromUtc
                        && x.PurchaseDate <= endUtc
                        && x.MoneyVipSnapshot > 0)
            .GroupBy(x => x.CharId!.Value)
            .Select(x => new
            {
                CharId = x.Key,
                MoneyVip = x.Sum(p => p.MoneyVipSnapshot)
            })
            .ToDictionaryAsync(x => x.CharId, x => x.MoneyVip);

        var eventRechargeMails = await db.Mails
            .AsNoTracking()
            .Where(x => x.CreatedAt >= fromUtc
                        && x.CreatedAt <= endUtc
                        && x.Title == "Phần thưởng nạp tiền")
            .Select(x => new
            {
                x.CharId,
                x.Rewards
            })
            .ToListAsync();

        foreach (var mail in eventRechargeMails)
        {
            var moneyVip = mail.Rewards
                .OfType<MailRewardMoneyVip>()
                .Sum(x => (long)x.MoneyVip);
            if (moneyVip <= 0)
            {
                continue;
            }

            purchaseMoneyVipByChar.TryGetValue(mail.CharId, out var currentMoneyVip);
            purchaseMoneyVipByChar[mail.CharId] = currentMoneyVip + moneyVip;
        }

        if (purchaseMoneyVipByChar.Count <= 0)
        {
            Log.Information("Open server recharge MoneyVip bonus: no eligible recharge found.");
            return;
        }

        var charIds = purchaseMoneyVipByChar.Keys.ToArray();
        var rewardedCharIds = await db.Mails
            .AsNoTracking()
            .Where(x => charIds.Contains(x.CharId) && x.Title == bonusTitle)
            .Select(x => x.CharId)
            .Distinct()
            .ToArrayAsync();
        var rewardedSet = rewardedCharIds.ToHashSet();
        var mails = new List<Mail>();
        foreach (var (charId, moneyVip) in purchaseMoneyVipByChar.OrderBy(x => x.Key))
        {
            if (rewardedSet.Contains(charId))
            {
                continue;
            }

            var rewardMoneyVip = checked((int)(moneyVip * multiplier));
            mails.Add(new Mail
            {
                CharId = charId,
                Title = bonusTitle,
                Content =
                    $"Cảm ơn đạo hữu đã ủng hộ giai đoạn open server. Bạn nhận thêm x{multiplier} Tiên Ngọc từ các gói nạp có thưởng Tiên Ngọc.",
                Rewards = new List<IMailReward>
                {
                    new MailRewardMoneyVip
                    {
                        MoneyVip = rewardMoneyVip
                    }
                }
            });
        }

        if (mails.Count <= 0)
        {
            Log.Information("Open server recharge MoneyVip bonus: all eligible characters were already rewarded.");
            return;
        }

        await db.Mails.AddRangeAsync(mails);
        await db.SaveChangesAsync();
        Log.Information("Open server recharge MoneyVip bonus sent: {MailCount} mails, {MoneyVip} MoneyVip.",
            mails.Count,
            Utils.Utils.FormatCount(mails.Sum(x => x.Rewards.OfType<MailRewardMoneyVip>().Sum(r => r.MoneyVip))));
    }

    private async Task AddEvents(AppDbContext db)
    {
        var charIds = await db.EventManagers
            .Select(x => x.CharId)
            .ToArrayAsync();
        
        foreach (var charId in charIds)
        {
            var events = EventService.CreateEvents();
            foreach (var @event in events)
            {
                @event.CharId = charId;
                await db.Events.AddAsync(@event);
            }
        }
        await db.SaveChangesAsync();
        Log.Information("Oke");
    }
}
