using MemoryPack;
using MTS.Game;
using MTS.Game.LuyenDan;
using MTS.Network.Models;
using MTS.Utils;

namespace MTS.Models;
[MemoryPackable]
public partial class MaterialShop : Shop
{
    public override void Reload(ReloadShopRequest request)
    {
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
        var materials = GameConfig.LuyenKhiResources.Materials
            .Where(x => tiers.Contains(x.Tier))
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
        var randomMaterial =
            new ItemRateRandomizer<List<ItemRef>>();
        foreach (var tierRate in tierRates)
        {
            if (materials.TryGetValue(tierRate.Tier, out var list))
            {
                randomMaterial.Add(list, tierRate.Rate);
            }
        }
        Items = Enumerable.Range(0, count).Select(_ => randomMaterial.Random().Random()).ToHashSet();
        LastReloadTime = DateTime.UtcNow;
        Dirty = true;
    }
    public override EShopType GetTypeShop()
    {
        return EShopType.ShopVatLieu;
    }
}