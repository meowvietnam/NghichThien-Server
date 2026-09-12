using MemoryPack;
namespace MTS.Network.Models;

[MemoryPackable]
public partial class GetClanShopRequest { }

[MemoryPackable]
public partial class GetClanShopResult : Result
{
    public MTS.Models.ClanShopItem[] Items { get; set; } = [];
    public long ContribPoints { get; set; }
    public int MaxQuantityPerRequest { get; set; }
    public int MaxItemsAddedPerDay { get; set; }
    public int ShopItemsAddedToday { get; set; }
}

[MemoryPackable]
public partial class BuyClanShopRequest
{
    public int ShopItemId { get; set; }
    // Number of items to buy.
    public int Quantity { get; set; }
}

[MemoryPackable]
public partial class BuyClanShopResult : Result
{
    public int ShopItemId { get; set; }
    public int Quantity { get; set; }
    public long Cost { get; set; }
    public long ContribPoints { get; set; }
    // Absolute inventory count after purchase, not a reward delta.
    public MTS.Models.ItemRef? InventoryItem { get; set; }
}

[MemoryPackable]
public partial class AddClanShopItemRequest
{
    public MTS.Models.ItemRef Item { get; set; } = new();
}

[MemoryPackable]
public partial class AddClanShopItemResult : Result
{
    public MTS.Models.ClanShopItem? ShopItem { get; set; }
    public MTS.Models.ItemRef? InventoryItem { get; set; }
    public int MaxItemsAddedPerDay { get; set; }
    public int ShopItemsAddedToday { get; set; }
}

[MemoryPackable]
public partial class RemoveClanShopItemRequest
{
    public int ShopItemId { get; set; }
}

[MemoryPackable]
public partial class RemoveClanShopItemResult : Result
{
    public int ShopItemId { get; set; }
    public MTS.Models.ItemRef? InventoryItem { get; set; }
}

[MemoryPackable]
public partial class ClanShopItemAdded
{
    public MTS.Models.ClanShopItem ShopItem { get; set; } = new();
}

[MemoryPackable]
public partial class ClanShopItemRemoved
{
    public int ClanId { get; set; }
    public int ShopItemId { get; set; }
}

[MemoryPackable]
public partial class ClanShopItemChanged
{
    public MTS.Models.ClanShopItem ShopItem { get; set; } = new();
}

[MemoryPackable]
public partial class ClanMemberPointsChanged
{
    public int ClanId { get; set; }
    public int CharId { get; set; }
    public long ContribPoints { get; set; }
}
