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
    private async Task AddEvents(AppDbContext db)
    {
        var charIds = await db.EventManagers
            .Select(x => x.CharId)
            .ToArrayAsync();
        
        foreach (var charId in charIds)
        {
            var @event = EventService.CreateEvents().First(x => x.GameEvent is EventShop);
            @event.CharId = charId;
            await db.Events.AddAsync(@event);
        }
        await db.SaveChangesAsync();
        Log.Information("Oke");
    }
}