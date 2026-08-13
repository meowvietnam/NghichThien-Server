using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
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

        productReward.ClaimNow(charData);
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