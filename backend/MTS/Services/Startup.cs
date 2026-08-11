using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.IAP;
using MTS.Models;
using MTS.Promotions;

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
        // await CreateData(db, stoppingToken);
    }

    private async Task CreateData(AppDbContext db, CancellationToken stoppingToken)
    {
        if (!await db.IapProducts.AnyAsync(stoppingToken))
        {
            var products = MemoryPackSerializer.Deserialize<List<IapProduct>>(File.ReadAllBytes("products.bin"));
            if (products != null) db.IapProducts.AddRange(products);
            await db.SaveChangesAsync(stoppingToken);
        }

        if (!await db.Promotions.AnyAsync(stoppingToken))
        {
            var startDate = GameConfig.GameReleaseDate.ToOffset(TimeSpan.Zero).DateTime;
            var endDate = startDate.AddYears(99);
            var promotion = new Promotion()
            {
                Name = "Ưu đãi nạp web",
                Description = "Nhận thêm 30% Tiên ngọc khi nạp qua web.",
                Store = Store.Web,
                PromotionApplicable = new MoneyVipPromotionApplicable()
                {
                    Percent = 30
                },
                StartDate = startDate,
                EndDate = endDate,
            };
            await db.Promotions.AddAsync(promotion, stoppingToken);
            await db.SaveChangesAsync(stoppingToken);
        }
        if (!await db.GiftCodes.AnyAsync(stoppingToken))
        {
            var giftcodes = new Giftcode[]
            {
                new ()
                {
                    Code = "nghichthienopenandroid",
                    Name = "Nghịch Thiên Open Android",
                    Description = "Quà mừng mở server dành cho người chơi Android.",
                    Rewards = new List<IMailReward>()
                    {
                        new MailRewardMoneyVip()
                        {
                            MoneyVip = 500
                        }
                    }
                },
                new ()
                {
                    Code = "nghichthienopenios",
                    Name = "Nghịch Thiên Open iOS",
                    Description = "Quà mừng mở server dành cho người chơi iOS.",
                    Rewards = new List<IMailReward>()
                    {
                        new MailRewardMoneyVip()
                        {
                            MoneyVip = 500
                        }
                    }
                },
                new ()
                {
                    Code = "nghichthienxinloi",
                    Name = "Nghịch Thiên Xin Lỗi",
                    Description = "Quà xin lỗi và cảm ơn đạo hữu đã thông cảm.",
                    Rewards = new List<IMailReward>()
                    {
                        new MailRewardMoneyVip()
                        {
                            MoneyVip = 1000
                        }
                    }
                }
            };
            await db.GiftCodes.AddRangeAsync(giftcodes, stoppingToken);
            await db.SaveChangesAsync(stoppingToken);
        }

    }
}
