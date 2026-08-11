using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.IAP;
using MTS.Models;

namespace MTS.Services;

public class PromotionService
{
    public static async Task<Promotion[]> GetActivePromotions(Store store, AppDbContext db)
    {
        var now = DateTime.UtcNow;
        return await db.Promotions
            .Where(x => x.Active && x.StartDate <= now && now <= x.EndDate)
            .ToArrayAsync();
    }
}