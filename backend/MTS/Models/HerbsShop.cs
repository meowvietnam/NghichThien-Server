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
        var herbs = GameConfig.LuyenDanResources.HerbDatas
            .Where(x => x.Tier <= Tier.NhiPham && !herbIdExclusives.Contains(x.Id))
            .OrderBy(x => Random.Shared.Next())
            .Take(Take())
            .Select(x => new ItemRef()
            {
                Type = ItemType.Herb,
                Id = (ushort)x.Id,
                Count = RandomCount()
            });
        items.AddRange(herbs);
        if (RandomUtils.RandomChance(50))
        {
            var danLus = GameConfig.LuyenDanResources.TempFurnaceDatas
                .Where(x => x.Tier <= Tier.NhiPham)
                .OrderBy(x => Random.Shared.Next())
                .Take(Take())
                .Select(x => new ItemRef()
                {
                    Type = ItemType.Furnace,
                    Id = (ushort)x.Id,
                    Count = RandomCount()
                });
            items.AddRange(danLus);
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