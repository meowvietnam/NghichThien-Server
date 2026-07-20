using MTS.Exceptions;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Game;
using MTS.Game.ItemProvider;
using MTS.Game.LuyenDan;
using MTS.Localization;
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
    public Shop ShopSkillAll { get; set; } = new SkillShop();
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
        var request2 = new ReloadShopRequest()
        {
            MonPhai = EMonPhai.None,
            ShopType = EShopType.Skill
        };
        ShopSkillAll.Reload(request2);
    }

    public async Task SaveChanges(AppDbContext db)
    {
        var shops = new[] { ShopDuocLieu, ShopVatLieu, ShopSkill, ShopSkillAll };
        var dirtyMask = (ShopDuocLieu.Dirty ? 1 : 0)
                        | (ShopVatLieu.Dirty ? 2 : 0)
                        | (ShopSkill.Dirty ? 4 : 0)
                        | (ShopSkillAll.Dirty ? 8 : 0);
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
            8 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            9 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            10 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            11 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            12 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopSkill, ShopSkill)
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            13 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)
                .SetProperty(x => x.ShopSkill, ShopSkill)
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            14 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)
                .SetProperty(x => x.ShopSkill, ShopSkill)
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            15 => await query.ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ShopDuocLieu, ShopDuocLieu)
                .SetProperty(x => x.ShopVatLieu, ShopVatLieu)
                .SetProperty(x => x.ShopSkill, ShopSkill)
                .SetProperty(x => x.ShopSkillAll, ShopSkillAll)),
            _ => 0
        };

        foreach (var shop in shops)
        {
            shop.Dirty = false;
        }
    }


    public Shop ReloadShop(ReloadShopRequest request)
    {
        var loadShop = new LoadShopRequest()
        {
            ShopType = request.ShopType,
            MonPhai = request.MonPhai,
        };
        var shop = GetShop(loadShop);
        shop.Reload(request);
        return shop;
    }

    public void CopyFrom(ShopManager clone)
    {
        ShopDuocLieu = clone.ShopDuocLieu;
        ShopVatLieu = clone.ShopVatLieu;
        ShopSkill = clone.ShopSkill;
    }

    public Shop GetShop(LoadShopRequest request)
    {
        var shopType = request.ShopType;
        var monPhai = request.MonPhai;
        switch (shopType)
        {
            case EShopType.ShopDuocLieu:
                return ShopDuocLieu;
            case EShopType.ShopVatLieu:
                return ShopVatLieu;
            case EShopType.Skill:
                if (monPhai == CharData.MonPhai)
                {
                    return ShopSkill;
                }
                if (monPhai == EMonPhai.None)
                {
                    return ShopSkillAll;
                }
                throw new InvalidException(Strings.ShopMonPhaiInvalid);
            default:
                throw new InvalidException(Strings.ShopTypeInvalid);
        }
    }

    public BuyAndSellResult BuyAndSell(BuyAndSell request)
    {
        var result = new BuyAndSellResult()
        {
            Success = true,
            MonPhai = request.MonPhai,
        };
        var inventory = CharData.InventoryManager;
        lock (inventory.SyncRoot)
        {
        var sellItems = request.SellItems.ToHashSet();
        var buyItems = request.BuyItems.ToHashSet();
        var totalSellAmount = 0;
        var totalBuyAmount = 0;
        var shopType = request.ShopType;
        var monPhai = request.MonPhai;
        var loadShop = new LoadShopRequest()
        {
            ShopType =  shopType,
            MonPhai = monPhai,
        };
        var shop = GetShop(loadShop);
        var items = shop.Items;
        var providers = new Dictionary<ItemType, IItemProvider>();
        foreach (var buyItem in buyItems)
        {
            if (buyItem.Count == 0)
            {
                throw new InvalidException(Strings.ShopItemQuantityInvalid);
            }

            if (!items.TryGetValue(buyItem, out var item))
            {
                throw new InvalidException(Strings.ShopItemNotFound);
            }

            var remaining = item.Count - buyItem.Count;
            if (remaining < 0)
            {
                throw new InvalidException(Strings.ShopItemQuantityInvalid);
            }

            var type = buyItem.Type;
            if (!providers.TryGetValue(type, out var provider))
            {
                provider = ItemProviderFactory.GetProvider(type, inventory)!;
                providers[type] = provider;
            }

            var buyPrice = provider.GetPriceItemBuy(buyItem.Id) * buyItem.Count;
            totalBuyAmount += buyPrice;
        }

        foreach (var sellItem in sellItems)
        {
            var id = sellItem.Id;
            var type = sellItem.Type;
            var count = sellItem.Count;
            if (count == 0)
            {
                throw new InvalidException(Strings.InventoryItemQuantityInvalid);
            }

            var itemCollection = inventory.GetItemCollection(type);
            if (itemCollection.Collection is HashSet<FurnaceData> furnaces)
            {
                if (count != 1)
                {
                    throw new InvalidException(Strings.InventoryItemQuantityInvalid);
                }

                var furnace = new FurnaceData()
                {
                    SelfId = id,
                };
                if (!furnaces.Contains(furnace))
                {
                    throw new InvalidException(Strings.InventoryItemNotFound);
                }

                if (!providers.TryGetValue(type, out var provider))
                {
                    provider = ItemProviderFactory.GetProvider(type, inventory)!;
                    providers[type] = provider;
                }

                var sellPrice = provider.GetPriceItemSell(id);
                totalSellAmount += sellPrice;
            }
            else if (itemCollection.Collection is HashSet<ItemRef> itemRefs)
            {
                if (!itemRefs.TryGetValue(sellItem, out var itemRef))
                {
                    throw new InvalidException(Strings.InventoryItemNotFound);
                }

                var remaining = itemRef.Count - count;
                if (remaining < 0)
                {
                    throw new InvalidException(Strings.InventoryItemQuantityInvalid);
                }

                if (!providers.TryGetValue(type, out var provider))
                {
                    provider = ItemProviderFactory.GetProvider(type, inventory)!;
                    providers[type] = provider;
                }

                var sellPrice = provider.GetPriceItemSell(id) * count;
                totalSellAmount += sellPrice;
            }
        }

        var money = CharData.Stats.LinhThach + totalSellAmount - totalBuyAmount;
        if (money < 0)
        {
            throw new InvalidException(Strings.NotEnoughMoney);
        }

        foreach (var buyItem in buyItems)
        {
            var count = buyItem.Count;
            var type = buyItem.Type;
            items.TryGetValue(buyItem, out var item);
            var remaining = item!.Count - count;
            var itemCollection = inventory.GetItemCollection(type);
            if (itemCollection.Collection is HashSet<FurnaceData>)
            {
                for (int i = 0; i < count; i++)
                {
                    var furnace = new FurnaceData()
                    {
                        CharId = CharId,
                        Id = buyItem.Id,
                        Durability = 100
                    };
                    inventory.AddFurnace(furnace);
                    result.Furnaces.Add(furnace);
                }
            }
            else if (itemCollection.Collection is HashSet<ItemRef> myItems)
            {
                var tracking = itemCollection.Tracking as Modify<ItemRef>;
                if (myItems.TryGetValue(buyItem, out var myItem))
                {
                    myItem.Count += count;
                    tracking.Update(myItem);
                    result.Items.Update(myItem);
                }
                else
                {
                    buyItem.CharId = CharId;
                    myItems.Add(buyItem);
                    tracking.Add(buyItem);
                    result.Items.Add(buyItem);
                }
            }

            item.Count = (ushort)remaining;
            if (item.Count == 0)
            {
                items.Remove(item);
            }

            shop.Dirty = true;
        }

        foreach (var sellItem in sellItems)
        {
            var id = sellItem.Id;
            var type = sellItem.Type;
            var count = sellItem.Count;
            var itemCollection = inventory.GetItemCollection(type);
            if (itemCollection.Collection is HashSet<FurnaceData> furnaces)
            {
                var tracking = itemCollection.Tracking as Modify<FurnaceData>;
                var furnaceRef = new FurnaceData()
                {
                    SelfId = id,
                };
                furnaces.TryGetValue(furnaceRef, out var furnace);
                furnaces.Remove(furnace);
                tracking.Delete(furnace);
                result.Furnaces.Delete(furnace);
            }
            else if (itemCollection.Collection is HashSet<ItemRef> myItems)
            {
                var tracking = itemCollection.Tracking as Modify<ItemRef>;
                if (myItems.TryGetValue(sellItem, out var myItem))
                {
                    var remaining = myItem.Count - count;
                    if (remaining <= 0)
                    {
                        myItems.Remove(myItem);
                        tracking.Delete(myItem);
                        result.Items.Delete(myItem);
                    }
                    else
                    {
                        myItem.Count = (ushort)remaining;
                        tracking.Update(myItem);
                        result.Items.Update(myItem);
                    }
                }
            }
        }

        CharData.Stats.SetLinhThach(money);
        result.ReloadShop = shop;
        result.TotalBuyAmount = totalBuyAmount;
        result.TotalSellAmount = totalSellAmount;
        result.LinhThach = money;
        var totalAmount = result.GetTotalAmount();
        string message;
        message = totalAmount >= 0 ? $"Giao dịch thành công! Bạn nhận được {totalAmount} Linh thạch!" 
            : $"Giao dịch thành công! Bạn phải trả {-totalAmount} Linh thạch!";
        result.Message = message;
        return result;
        }
    }
}
