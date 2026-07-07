using MemoryPack;
using MTS.Game;
using MTS.Game.LuyenDan;
using MTS.Network.Models;

namespace MTS.Models;
[MemoryPackable]
public partial class MaterialShop : Shop
{
    public override void Reload(ReloadShopRequest request)
    {
        var materials = GameConfig.LuyenKhiResources.Materials;
        Items = materials
            .Where(x => x.Tier <= Tier.NhiPham)
            .OrderBy(x => Random.Shared.Next())
            .Take(Take())
            .Select(x => new ItemRef()
            {
                Type = ItemType.Material,
                Id = (ushort)x.Id,
                Count = RandomCount()
            })
            .ToHashSet();
        LastReloadTime = DateTime.UtcNow;
        Dirty = true;
    }
    public override EShopType GetTypeShop()
    {
        return EShopType.ShopVatLieu;
    }
}