using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Game.BaseSkill;
using MTS.Game.LuyenDan;
using MTS.Game.LuyenKhi;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.Clan;

public static partial class ClanManager
{
    private static readonly Dictionary<Tier, long> ClanHerbPrices = new()
    {
        { Tier.NhatPham, 50 },
        { Tier.NhiPham, 100 },
        { Tier.TamPham, 200 },
        { Tier.TuPham, 500 },
        { Tier.NguPham, 1_000 },
        { Tier.LucPham, 2_000 }
    };

    private static readonly Dictionary<Tier, long> ClanMaterialPrices = new()
    {
        { Tier.NhiPham, 50 },
        { Tier.TamPham, 100 },
        { Tier.TuPham, 200 },
        { Tier.NguPham, 500 },
        { Tier.LucPham, 1_000 }
    };

    private static readonly Dictionary<Tier, long> ClanAuctionTierPrices = new()
    {
        { Tier.TuPham, 2_000 },
        { Tier.NguPham, 5_000 },
        { Tier.LucPham, 10_000 }
    };

    // Số lượt treo vật phẩm tối đa MỖI NGÀY của CẢ TÔNG MÔN (dùng chung cho mọi thành viên),
    // không còn là hạn mức riêng của từng người.
    public static int GetMaxShopSlots(int clanLevel)
    {
        return Math.Max(1, clanLevel) * 3;
    }

    // Lượt treo vật phẩm trong kho tông môn reset theo NGÀY GIỜ VIỆT NAM (UTC+7), không phải theo
    // ngày UTC - tức là mốc reset là 00:00 giờ Việt Nam (tương đương 17:00 UTC ngày hôm trước).
    private static readonly TimeSpan ShopResetTimeZoneOffset = TimeSpan.FromHours(7);

    private static DateTime GetShopResetDate(DateTime utcNow)
    {
        return (utcNow + ShopResetTimeZoneOffset).Date;
    }

    public static GetClanShopResult GetShop(ClanReference reference, int charId)
    {
        var clan = reference.Clan;
        var member = clan.Members.FirstOrDefault(x => x.CharId == charId)
            ?? throw new InvalidException("Chưa gia nhập tông môn");
        var today = GetShopResetDate(DateTime.UtcNow);
        // Lượt treo vật phẩm được tính chung cho cả tông môn, không tách riêng theo từng thành viên.
        var addedToday = clan.LastShopItemAddedAt.Date == today
            ? clan.ShopItemsAddedToday
            : 0;
        return new GetClanShopResult
        {
            Success = true,
            Items = clan.ShopItems.ToArray(),
            ContribPoints = member.ContribPoints,
            MaxQuantityPerRequest = Config.MaxShopQuantityPerRequest,
            ShopItemsAddedToday = addedToday
        };
    }

    // Caller must hold Client.CharacterLock.
    public static async Task<AddClanShopItemResult> AddShopItem(
        ClanReference reference, CharacterData character, AddClanShopItemRequest request)
    {
        if (character.Progression.MaxLevel < 4)
        {
            throw new InvalidException("Yêu cầu đạt Trúc cơ sơ kỳ");
        }
        var deposit = request.Item.DeepClone();
        ValidateShopItem(deposit);
        var inventory = character.InventoryManager;
        var requestedItem = deposit.DeepClone();
        var depositFurnace = GetDepositFurnace(deposit, inventory);
        if (depositFurnace != null)
        {
            deposit = new ItemRef(depositFurnace.Id, ItemType.Furnace, 1);
        }
        var price = GetClanShopPrice(deposit, depositFurnace);

        var clan = reference.Clan;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var member = clan.Members.FirstOrDefault(x => x.CharId == character.Id)
                ?? throw new InvalidException("Chưa gia nhập tông môn");
            if (!member.HasPermission(ClanPermission.AddShopItem))
                throw new InvalidException("Không có quyền thêm vật phẩm vào kho tông môn");
            var maxShopSlots = GetMaxShopSlots(clan.Level);
            if (maxShopSlots <= 0)
                throw new InvalidException("Kho tông môn đang tạm khóa thêm vật phẩm");

            // Lượt treo vật phẩm được tính chung cho cả tông môn, không tách riêng theo từng thành viên.
            var today = GetShopResetDate(DateTime.UtcNow);
            var addedToday = clan.LastShopItemAddedAt.Date == today
                ? clan.ShopItemsAddedToday
                : 0;
            int newAddedToday;
            try
            {
                newAddedToday = checked(addedToday + deposit.Count);
            }
            catch (OverflowException)
            {
                throw new InvalidException("Số lượng vật phẩm thêm vào kho hôm nay vượt giới hạn");
            }
            if (newAddedToday > maxShopSlots)
                throw new InvalidException("Số lượng vật phẩm thêm vào kho hôm nay vượt giới hạn");

            ClanShopItem shopItem = null!;
            ItemRef? inventoryItem;
            int remaining;
            lock (inventory.SyncRoot)
            {
                if (depositFurnace != null)
                {
                    if (!inventory.Furnaces.Contains(depositFurnace))
                        throw new InvalidException("Vật phẩm không đủ trong túi");
                    remaining = 0;
                }
                else
                {
                    var collection = (HashSet<ItemRef>)inventory.GetItemCollection(deposit.Type).Collection;
                    if (!collection.TryGetValue(deposit, out var owned) || owned.Count < deposit.Count)
                        throw new InvalidException("Vật phẩm không đủ trong túi");
                    remaining = owned.Count - deposit.Count;
                }
            }

            var createdAt = DateTime.UtcNow;
            await ExecuteClanTransactionAsync(async db =>
            {
                if (shopItem?.Id > 0 &&
                    await db.ClanShopItems.AsNoTracking().AnyAsync(x => x.Id == shopItem.Id && x.ClanId == clanId) &&
                    await db.Clans.AsNoTracking().AnyAsync(x => x.Id == clanId &&
                        x.ShopItemsAddedToday == newAddedToday && x.LastShopItemAddedAt == today))
                {
                    return true;
                }

                var updated = await db.Clans
                    .Where(x => x.Id == clanId &&
                                x.ShopItemsAddedToday == clan.ShopItemsAddedToday &&
                                x.LastShopItemAddedAt == clan.LastShopItemAddedAt)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.ShopItemsAddedToday, newAddedToday)
                        .SetProperty(c => c.LastShopItemAddedAt, today));
                if (updated != 1)
                    throw new InvalidException("Dữ liệu tông môn đã thay đổi, vui lòng thử lại");

                shopItem = new ClanShopItem
                {
                    ClanId = clanId,
                    OwnerCharId = character.Id,
                    Item = deposit.DeepClone(),
                    ItemDurability = depositFurnace?.Durability ?? 0,
                    Price = price,
                    CreatedAt = createdAt
                };
                await db.ClanShopItems.AddAsync(shopItem);
                await db.SaveChangesAsync();
                return true;
            });

            lock (inventory.SyncRoot)
            {
                if (depositFurnace != null)
                {
                    inventory.RemoveFurnace(requestedItem.Id);
                    inventoryItem = null;
                }
                else
                {
                    inventory.Decrease(deposit);
                    inventoryItem = remaining > 0 ? new ItemRef(deposit.Id, deposit.Type, remaining) : null;
                }
            }

            clan.ShopItemsAddedToday = newAddedToday;
            clan.LastShopItemAddedAt = today;
            clan.ShopItems.Add(shopItem);
            NotifyClanShopItemAdded(clanId, shopItem);
            return new AddClanShopItemResult
            {
                Success = true,
                ShopItem = shopItem,
                InventoryItem = inventoryItem,
                ShopItemsAddedToday = newAddedToday
            };
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    public static async Task<RemoveClanShopItemResult> RemoveShopItem(
        ClanReference reference, CharacterData character, RemoveClanShopItemRequest request)
    {
        var clan = reference.Clan;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var member = clan.Members.FirstOrDefault(x => x.CharId == character.Id)
                ?? throw new InvalidException("Chưa gia nhập tông môn");
            if (!member.HasPermission(ClanPermission.RemoveShopItem))
                throw new InvalidException("Không có quyền xóa vật phẩm khỏi kho tông môn");

            var shopItem = clan.ShopItems.FirstOrDefault(x => x.Id == request.ShopItemId)
                ?? throw new InvalidException("Vật phẩm không có trong kho tông môn");
            if (shopItem.OwnerCharId != character.Id)
                throw new InvalidException("Chỉ chủ nhân vật phẩm mới được xóa vật phẩm này");

            var returnedItem = shopItem.Item.DeepClone();
            var returnedFurnace = CreateFurnaceFromShopItem(character.Id, shopItem);
            var inventory = character.InventoryManager;
            ItemRef inventoryItem;
            int total;
            lock (inventory.SyncRoot)
            {
                if (returnedFurnace != null)
                {
                    inventoryItem = null;
                    total = 0;
                }
                else
                {
                    var collection = (HashSet<ItemRef>)inventory.GetItemCollection(returnedItem.Type).Collection;
                    collection.TryGetValue(returnedItem, out var owned);
                    if ((long)(owned?.Count ?? 0) + returnedItem.Count > int.MaxValue)
                        throw new InvalidException("Số lượng vật phẩm trong túi vượt giới hạn");
                    total = (owned?.Count ?? 0) + returnedItem.Count;
                    inventoryItem = new ItemRef(returnedItem.Id, returnedItem.Type, total);
                }
            }

            await using (var db = await Program.CreateDbAsync())
            {
                // A single ExecuteDelete is already atomic; no user transaction is needed.
                var deleted = await db.ClanShopItems
                    .Where(x => x.Id == shopItem.Id && x.ClanId == clanId && x.OwnerCharId == character.Id)
                    .ExecuteDeleteAsync();
                if (deleted != 1 && await db.ClanShopItems.AnyAsync(x => x.Id == shopItem.Id && x.ClanId == clanId))
                    throw new InvalidException("Vật phẩm không có trong kho tông môn");
            }

            lock (inventory.SyncRoot)
            {
                if (returnedFurnace != null)
                    inventory.AddFurnace(returnedFurnace);
                else
                    inventory.Add(returnedItem);
            }

            clan.ShopItems.RemoveAll(x => x.Id == shopItem.Id);
            NotifyClanShopItemRemoved(clanId, shopItem.Id);
            return new RemoveClanShopItemResult
            {
                Success = true,
                ShopItemId = shopItem.Id,
                InventoryItem = inventoryItem,
                InventoryFurnace = returnedFurnace
            };
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    public static async Task<BuyClanShopResult> BuyShopItem(
        ClanReference reference, CharacterData character, BuyClanShopRequest request)
    {
        if (request.Quantity <= 0 || request.Quantity > Config.MaxShopQuantityPerRequest)
            throw new InvalidException("Số lượng mua không hợp lệ");

        var clan = reference.Clan;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var member = clan.Members.FirstOrDefault(x => x.CharId == character.Id)
                ?? throw new InvalidException("Chưa gia nhập tông môn");
            var offer = clan.ShopItems.FirstOrDefault(x => x.Id == request.ShopItemId)
                ?? throw new InvalidException("Vật phẩm không có trong kho tông môn");
            if (offer.OwnerCharId == character.Id)
                throw new InvalidException("Không thể mua vật phẩm do chính mình thêm vào kho");
            if (offer.Count < request.Quantity)
                throw new InvalidException("Kho tông môn không đủ số lượng vật phẩm");

            var reward = new ItemRef(offer.ItemId, offer.ItemType, request.Quantity);
            var rewardFurnace = CreateFurnaceFromShopItem(character.Id, offer);
            ValidateShopItem(reward);
            if (offer.Price <= 0)
                throw new InvalidException("Giá vật phẩm trong kho tông môn không hợp lệ");
            long cost;
            try
            {
                cost = checked(offer.Price * request.Quantity);
            }
            catch (OverflowException)
            {
                throw new InvalidException("Số lượng hoặc giá mua vượt giới hạn");
            }

            if (member.ContribPoints < cost)
                throw new InvalidException("Không đủ điểm cống hiến");

            var inventory = character.InventoryManager;
            var removeOffer = offer.Count == request.Quantity;
            int total;
            lock (inventory.SyncRoot)
            {
                if (rewardFurnace != null)
                {
                    total = 0;
                }
                else
                {
                    var collection = (HashSet<ItemRef>)inventory.GetItemCollection(reward.Type).Collection;
                    collection.TryGetValue(reward, out var owned);
                    if ((long)(owned?.Count ?? 0) + reward.Count > int.MaxValue)
                        throw new InvalidException("Số lượng vật phẩm trong túi vượt giới hạn");
                    total = (owned?.Count ?? 0) + reward.Count;
                }
            }

            var points = member.ContribPoints - cost;
            var newShopCount = offer.Count - request.Quantity;
            await ExecuteClanTransactionAsync(async db =>
            {
                var alreadyCommitted = await db.ClanMembers.AsNoTracking().AnyAsync(x =>
                        x.ClanId == clanId && x.CharId == character.Id && x.ContribPoints == points) &&
                    (newShopCount == 0
                        ? !await db.ClanShopItems.AsNoTracking().AnyAsync(x => x.Id == offer.Id && x.ClanId == clanId)
                        : await db.ClanShopItems.AsNoTracking().AnyAsync(x =>
                            x.Id == offer.Id && x.ClanId == clanId && x.Count == newShopCount));
                if (alreadyCommitted) return true;

                var updated = await db.ClanMembers
                    .Where(x => x.ClanId == clanId && x.CharId == character.Id &&
                                x.ContribPoints == member.ContribPoints && x.ContribPoints >= cost)
                    .ExecuteUpdateAsync(x => x.SetProperty(m => m.ContribPoints, points));
                if (updated != 1)
                    throw new InvalidException("Không đủ điểm cống hiến hoặc đã rời tông môn");

                updated = await db.ClanShopItems
                    .Where(x => x.Id == offer.Id && x.ClanId == clanId &&
                        x.Count == offer.Count && x.Count >= request.Quantity)
                    .ExecuteUpdateAsync(x => x.SetProperty(s => s.Count, newShopCount));
                if (updated != 1)
                    throw new InvalidException("Kho tông môn không đủ số lượng vật phẩm");
                if (newShopCount == 0)
                {
                    await db.ClanShopItems
                        .Where(x => x.Id == offer.Id && x.ClanId == clanId && x.Count == 0)
                        .ExecuteDeleteAsync();
                }

                return true;
            });

            lock (inventory.SyncRoot)
            {
                if (rewardFurnace != null)
                    inventory.AddFurnace(rewardFurnace);
                else
                    inventory.Add(reward);
                member.ContribPoints = points;
                if (removeOffer)
                {
                    clan.ShopItems.RemoveAll(x => x.Id == offer.Id);
                }
                else
                {
                    offer.Count = newShopCount;
                }

            }

            var result = new BuyClanShopResult
            {
                Success = true, ShopItemId = request.ShopItemId, Quantity = request.Quantity,
                Cost = cost, ContribPoints = points,
                InventoryItem = rewardFurnace == null ? new ItemRef(reward.Id, reward.Type, total) : null,
                InventoryFurnace = rewardFurnace
            };

            NotifyClanMemberPointsChanged(clanId, character.Id, member.ContribPoints);
            if (removeOffer)
            {
                NotifyClanShopItemRemoved(clanId, offer.Id);
            }
            else
            {
                NotifyClanShopItemChanged(clanId, offer);
            }

            return result;
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    private static void ValidateShopItem(ItemRef item)
    {
        if (item.Count <= 0 ||
            item.Type is not (ItemType.Herb or ItemType.Furnace or ItemType.LinhDan or ItemType.ThanThong
                or ItemType.CongPhap or ItemType.Recipe or ItemType.Material))
            throw new InvalidException("Cấu hình vật phẩm kho tông môn không hợp lệ");
    }

    private static long GetClanShopPrice(ItemRef item, FurnaceData? furnace = null)
    {
        try
        {
            var price = item.Type switch
            {
                ItemType.CongPhap => GetClanSkillPrice(GameConfig.NDCPTTManager.GetCongPhapWithId(item.Id)?.eTierSkill,
                    nhanGiaiPrice: 200, diaGiaiPrice: 2_500, thienGiaiPrice: 5_000),
                ItemType.ThanThong => GetClanSkillPrice(GameConfig.NDCPTTManager.GetThanThongWithId(item.Id)?.eTierSkill,
                    nhanGiaiPrice: 100, diaGiaiPrice: 2_000, thienGiaiPrice: 4_000),
                ItemType.Herb => GetClanTierPrice(
                    LuyenDanResources.Instance.FindHerb(item.Id)?.Tier,
                    ClanHerbPrices),
                ItemType.Material => GetClanTierPrice(
                    GameConfig.LuyenKhiResources.FindMaterial(item.Id)?.Tier,
                    ClanMaterialPrices),
                ItemType.LinhDan => GetClanTierPrice(
                    LuyenDanResources.Instance.FindLinhDan(item.Id)?.Tier,
                    ClanAuctionTierPrices),
                ItemType.Recipe => GetClanTierPrice(
                    LuyenDanResources.Instance.FindRecipe(item.Id)?.Tier,
                    ClanAuctionTierPrices) * 2,
                ItemType.Furnace => GetClanFurnacePrice(furnace),
                _ => 0
            };
            if (price <= 0)
                throw new InvalidException("Giá vật phẩm kho tông môn không hợp lệ");
            return price;
        }
        catch (InvalidException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new InvalidException("Không thể xác định giá vật phẩm kho tông môn");
        }
    }

    private static long GetClanSkillPrice(ETierSkill? tier, long nhanGiaiPrice, long diaGiaiPrice, long thienGiaiPrice)
    {
        return tier switch
        {
            ETierSkill.NhanGiaiHa or ETierSkill.NhanGiaiTrung or ETierSkill.NhanGiaiThuong => nhanGiaiPrice,
            ETierSkill.DiaGiaiHa or ETierSkill.DiaGiaiTrung or ETierSkill.DiaGiaiThuong => diaGiaiPrice,
            ETierSkill.ThienGiaiHa or ETierSkill.ThienGiaiTrung or ETierSkill.ThienGiaiThuong => thienGiaiPrice,
            _ => 0
        };
    }

    private static long GetClanTierPrice(Tier? tier, IReadOnlyDictionary<Tier, long> prices)
    {
        if (!tier.HasValue) return 0;
        if (prices.TryGetValue(tier.Value, out var price)) return price;

        var higherTier = NextTier(tier.Value);
        if (!higherTier.HasValue) return 0;

        var higherPrice = GetClanTierPrice(higherTier.Value, prices);
        return higherPrice <= 0 ? 0 : higherPrice / 2;
    }

    private static Tier? NextTier(Tier tier)
    {
        return tier switch
        {
            Tier.NhatPham => Tier.NhiPham,
            Tier.NhiPham => Tier.TamPham,
            Tier.TamPham => Tier.TuPham,
            Tier.TuPham => Tier.NguPham,
            Tier.NguPham => Tier.LucPham,
            _ => null
        };
    }

    private static FurnaceData? GetDepositFurnace(ItemRef item, InventoryManager inventory)
    {
        if (item.Type != ItemType.Furnace) return null;
        if (item.Count != 1)
            throw new InvalidException("Mỗi lần chỉ được thêm 1 đan lô vào kho tông môn");

        lock (inventory.SyncRoot)
        {
            var furnaceRef = new FurnaceData { SelfId = item.Id };
            if (!inventory.Furnaces.TryGetValue(furnaceRef, out var furnace))
                throw new InvalidException("Vật phẩm không đủ trong túi");
            return furnace.DeepClone();
        }
    }

    private static long GetClanFurnacePrice(FurnaceData? furnace)
    {
        if (furnace == null) return 0;

        var tier = LuyenDanResources.Instance.FindItem(furnace)?.Tier;
        var fullDurabilityPrice = GetClanTierPrice(tier, ClanAuctionTierPrices);
        if (fullDurabilityPrice <= 0) return 0;

        var durability = Math.Clamp((int)furnace.Durability, 0, 100);
        return Math.Max(1, fullDurabilityPrice * durability / 100);
    }

    private static FurnaceData? CreateFurnaceFromShopItem(int charId, ClanShopItem shopItem)
    {
        if (shopItem.ItemType != ItemType.Furnace) return null;

        return new FurnaceData
        {
            CharId = charId,
            Id = shopItem.ItemId,
            Durability = shopItem.ItemDurability > 0 ? shopItem.ItemDurability : (sbyte)100
        };
    }

    private static ClanCreationGate AcquireGate(int clanId)
    {
        return CreationLocks.AddOrUpdate(clanId,
            _ => new ClanCreationGate { RefCount = 1 },
            (_, existing) => { Interlocked.Increment(ref existing.RefCount); return existing; });
    }

    private static void ReleaseGate(int clanId, ClanCreationGate gate)
    {
        gate.Semaphore.Release();
        if (Interlocked.Decrement(ref gate.RefCount) == 0)
            ((ICollection<KeyValuePair<int, ClanCreationGate>>)CreationLocks)
                .Remove(new KeyValuePair<int, ClanCreationGate>(clanId, gate));
    }

    private static void NotifyClanMemberPointsChanged(int clanId, int charId, long contribPoints)
    {
        if (!Observers.TryGetValue(clanId, out var observer)) return;
        try
        {
            observer.SendAll(MessageTag.ClanMemberPointsChanged, new ClanMemberPointsChanged
            {
                ClanId = clanId, CharId = charId, ContribPoints = contribPoints
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify clan member points for {ClanId}/{CharId}", clanId, charId);
        }
    }

    private static void NotifyClanShopItemAdded(int clanId, ClanShopItem shopItem)
    {
        if (!Observers.TryGetValue(clanId, out var observer)) return;
        try
        {
            observer.SendAll(MessageTag.ClanShopItemAdded, new ClanShopItemAdded { ShopItem = shopItem });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify clan shop item added for {ClanId}/{ShopItemId}", clanId, shopItem.Id);
        }
    }

    private static void NotifyClanShopItemRemoved(int clanId, int shopItemId)
    {
        if (!Observers.TryGetValue(clanId, out var observer)) return;
        try
        {
            observer.SendAll(MessageTag.ClanShopItemRemoved, new ClanShopItemRemoved
            {
                ClanId = clanId,
                ShopItemId = shopItemId
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify clan shop item removed for {ClanId}/{ShopItemId}", clanId, shopItemId);
        }
    }

    private static void NotifyClanShopItemChanged(int clanId, ClanShopItem shopItem)
    {
        if (!Observers.TryGetValue(clanId, out var observer)) return;
        try
        {
            observer.SendAll(MessageTag.ClanShopItemChanged, new ClanShopItemChanged { ShopItem = shopItem });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to notify clan shop item changed for {ClanId}/{ShopItemId}", clanId, shopItem.Id);
        }
    }
}
