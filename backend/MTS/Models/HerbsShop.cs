using MemoryPack;
using MTS.Game;
using MTS.Game.LuyenDan;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class HerbsShop : Shop
{
    public override void Reload(ReloadShopRequest request)
    {
        var items = new HashSet<ItemRef>();
        var linhDanIdExclusives = GameConfig.LuyenDanResources.LinhDans
            .Where(x => x.ETypeDan == ETypeDan.DanDotPha)
            .Select(x => x.Id)
            .ToHashSet();
        var herbIdExclusives = GameConfig.LuyenDanResources.Recipes
            .Where(x => linhDanIdExclusives.Contains(x.LinhDanId))
            .SelectMany(x => x.Recipes)
            .SelectMany(x => x.GetListHerbs())
            .Select(x => x.Id)
            .ToHashSet();
        var tierRates = new[]
        {
            new
            {
                Tier = Tier.NhatPham,
                Rate = 60
            },
            new
            {
                Tier = Tier.NhiPham,
                Rate = 40
            },
        };
        var tiers = tierRates.Select(x => x.Tier).ToArray();
        var count = Take();
        var danLuCount = 1;
        var herbCount = count - danLuCount;
        var herbs = GameConfig.LuyenDanResources.HerbDatas
            .Where(x => tiers.Contains(x.Tier) && !herbIdExclusives.Contains(x.Id))
            .Select(x => new
            {
                Tier = x.Tier,
                Ref = new ItemRef()
                {
                    Type = ItemType.Herb,
                    Id = (ushort)x.Id,
                    Count = RandomCount()
                }
            })
            .GroupBy(x => x.Tier)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Ref).ToList());
        var randomHerb =
            new ItemRateRandomizer<List<ItemRef>>();
        foreach (var tierRate in tierRates)
        {
            if (herbs.TryGetValue(tierRate.Tier, out var list))
            {
                randomHerb.Add(list, tierRate.Rate);
            }
        }
        for (int i = 0; i < herbCount; i++)
        {
            var list = randomHerb.Random();
            var item = list.Random();
            list.Remove(item);
            if (list.Count == 0)
            {
                randomHerb.Remove(list);
            }
            items.Add(item);
        }
        var danLus = GameConfig.LuyenDanResources.TempFurnaceDatas
            .Where(x =>  tiers.Contains(x.Tier) )
            .Select(x => new
            {
                Tier = x.Tier,
                Ref = new ItemRef()
                {
                    Type = ItemType.Furnace,
                    Id = (ushort)x.Id,
                    Count = RandomCount()
                }
            })
            .GroupBy(x => x.Tier)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Ref).ToList());
        var randomFurnace =
            new ItemRateRandomizer<List<ItemRef>>();
        foreach (var tierRate in tierRates)
        {
            if (danLus.TryGetValue(tierRate.Tier, out var list))
            {
                randomFurnace.Add(list, tierRate.Rate);
            }
        }
        
        for (int i = 0; i < danLuCount; i++)
        {
            if (RandomUtils.RandomChance(100))
            {
                var list = randomFurnace.Random();
                var item = list.Random();
                list.Remove(item);
                if (list.Count == 0)
                {
                    randomFurnace.Remove(list);
                }
                items.Add(item);
            }
            else
            {
                var list = randomHerb.Random();
                var item = list.Random();
                list.Remove(item);
                if (list.Count == 0)
                {
                    randomHerb.Remove(list);
                }
                items.Add(item);
            }
        }

        Items = items;
        LastReloadTime = DateTime.UtcNow;
        Dirty = true;
    }

    public override EShopType GetTypeShop()
    {
        return EShopType.ShopDuocLieu;
    }
}