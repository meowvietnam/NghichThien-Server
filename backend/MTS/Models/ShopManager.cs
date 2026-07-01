using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.LuyenDan;
using MTS.Network.Models;
using Newtonsoft.Json;
using MTS.Utils;

namespace MTS.Models;

[MemoryPackable]
public partial class ShopManager
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    public Shop ShopDuocLieu { get; private set; } = new HerbsShop();
    public Shop ShopVatLieu { get; private set; } = new MaterialShop();
    public Shop ShopSkill { get; private set; } = new SkillShop();

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }


    public bool IsDefault()
    {
        return CharId == 0;
    }

    public void ReloadAll()
    {
        var shops = new[] { ShopDuocLieu, ShopVatLieu, ShopSkill };
        var monPhai = CharData.MonPhai;
        foreach (var shop in shops)
        {
            var request = new ReloadShopRequest()
            {
                MonPhai = monPhai,
                ShopType = shop.GetTypeShop(),
            };
            shop.Reload(request);
        }
    }

    public async Task SaveChanges(AppDbContext db)
    {
        var shops = new[] { ShopDuocLieu, ShopVatLieu, ShopSkill };
        var dirtyMask = (ShopDuocLieu.Dirty ? 1 : 0)
                        | (ShopVatLieu.Dirty ? 2 : 0)
                        | (ShopSkill.Dirty ? 4 : 0);
        if (dirtyMask == 0)
        {
            return;
        }

        var query = db.ShopManagers.Where(x => x.CharId == CharId);
        _ = dirtyMask switch
        {
            1 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)),
            2 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)),
            3 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)),
            4 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopSkill, ShopSkill)),
            5 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)
                .SetProperty(x => x.ShopSkill, ShopSkill)),
            6 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)
                .SetProperty(x => x.ShopSkill, ShopSkill)),
            7 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)
                .SetProperty(x => x.ShopSkill, ShopSkill)),
            _ => 0
        };

        foreach (var shop in shops)
        {
            shop.Dirty = false;
        }
    }


    public Shop ReloadShop(EShopType shopType)
    {
        var shops = new[] { ShopDuocLieu, ShopVatLieu, ShopSkill };
        var shop = shops.First(x => x.GetTypeShop() == shopType);
        var monPhai = CharData.MonPhai;
        var request = new ReloadShopRequest()
        {
            MonPhai = monPhai,
            ShopType = shopType,
        };
        shop.Reload(request);
        return shop;
    }

    public void CopyFrom(ShopManager clone)
    {
        ShopDuocLieu = clone.ShopDuocLieu;
        ShopVatLieu = clone.ShopVatLieu;
        ShopSkill = clone.ShopSkill;
    }

    public Shop GetShop(EShopType shopType)
    {
        var shops = new[] { ShopDuocLieu, ShopVatLieu, ShopSkill };
        return shops.First(x => x.GetTypeShop() == shopType);
    }
}
