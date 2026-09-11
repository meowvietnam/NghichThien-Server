using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.Clan;

public static partial class ClanManager
{
    public static GetClanShopResult GetShop(ClanReference reference, int charId)
    {
        var member = reference.Clan.Members.FirstOrDefault(x => x.CharId == charId)
            ?? throw new InvalidException("Chưa gia nhập tông môn");
        var today = DateTime.UtcNow.Date;
        return new GetClanShopResult
        {
            Success = true,
            Items = reference.Clan.ShopItems.ToArray(),
            ContribPoints = member.ContribPoints,
            MaxQuantityPerRequest = Config.MaxShopQuantityPerRequest,
            MaxItemsAddedPerDay = Config.MaxShopItemsAddedPerDay,
            ShopItemsAddedToday = member.LastShopItemAddedAt.Date == today ? member.ShopItemsAddedToday : 0
        };
    }

    // Caller must hold Client.CharacterLock.
    public static async Task<AddClanShopItemResult> AddShopItem(
        ClanReference reference, CharacterData character, AddClanShopItemRequest request)
    {
        var deposit = request.Item.DeepClone();
        ValidateShopItem(deposit, request.Price);

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
            if (Config.MaxShopItemsAddedPerDay <= 0)
                throw new InvalidException("Kho tông môn đang tạm khóa thêm vật phẩm");

            var today = DateTime.UtcNow.Date;
            var addedToday = member.LastShopItemAddedAt.Date == today
                ? member.ShopItemsAddedToday
                : 0;
            if ((long)addedToday + deposit.Count > Config.MaxShopItemsAddedPerDay)
                throw new InvalidException("Đã đạt giới hạn thêm vật phẩm vào kho hôm nay");

            var inventory = character.InventoryManager;
            await using var db = await Program.CreateDbAsync();
            ClanShopItem shopItem = null!;
            ItemRef? inventoryItem;
            lock (inventory.SyncRoot)
            {
                var collection = (HashSet<ItemRef>)inventory.GetItemCollection(deposit.Type).Collection;
                if (!collection.TryGetValue(deposit, out var owned) || owned.Count < deposit.Count)
                    throw new InvalidException("Vật phẩm không đủ trong túi");

                var remaining = owned.Count - deposit.Count;
                db.Database.CreateExecutionStrategy().ExecuteInTransaction(() =>
                {
                    db.ChangeTracker.Clear();
                    var updated = db.ClanMembers
                        .Where(x => x.ClanId == clanId && x.CharId == character.Id &&
                                    x.ShopItemsAddedToday == member.ShopItemsAddedToday &&
                                    x.LastShopItemAddedAt == member.LastShopItemAddedAt)
                        .ExecuteUpdate(x => x
                            .SetProperty(m => m.ShopItemsAddedToday, addedToday + deposit.Count)
                            .SetProperty(m => m.LastShopItemAddedAt, today));
                    if (updated != 1)
                        throw new InvalidException("Dữ liệu tông môn đã thay đổi, vui lòng thử lại");

                    shopItem = new ClanShopItem
                    {
                        ClanId = clanId,
                        OwnerCharId = character.Id,
                        Item = deposit.DeepClone(),
                        Price = request.Price,
                        CreatedAt = DateTime.UtcNow
                    };
                    db.ClanShopItems.Add(shopItem);
                    db.SaveChanges();
                }, () => shopItem.Id > 0 &&
                    db.ClanShopItems.AsNoTracking().Any(x => x.Id == shopItem.Id && x.ClanId == clanId) &&
                    db.ClanMembers.AsNoTracking().Any(x => x.ClanId == clanId && x.CharId == character.Id &&
                        x.ShopItemsAddedToday == addedToday + deposit.Count && x.LastShopItemAddedAt == today));

                inventory.Decrease(deposit);
                inventoryItem = remaining > 0 ? new ItemRef(deposit.Id, deposit.Type, remaining) : null;
            }

            member.ShopItemsAddedToday = addedToday + deposit.Count;
            member.LastShopItemAddedAt = today;
            clan.ShopItems.Add(shopItem);
            NotifyClanShopItemAdded(clanId, shopItem);
            return new AddClanShopItemResult
            {
                Success = true,
                ShopItem = shopItem,
                InventoryItem = inventoryItem,
                MaxItemsAddedPerDay = Config.MaxShopItemsAddedPerDay,
                ShopItemsAddedToday = member.ShopItemsAddedToday
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
            var inventory = character.InventoryManager;
            await using var db = await Program.CreateDbAsync();
            ItemRef inventoryItem;
            lock (inventory.SyncRoot)
            {
                var collection = (HashSet<ItemRef>)inventory.GetItemCollection(returnedItem.Type).Collection;
                collection.TryGetValue(returnedItem, out var owned);
                if ((long)(owned?.Count ?? 0) + returnedItem.Count > int.MaxValue)
                    throw new InvalidException("Số lượng vật phẩm trong túi vượt giới hạn");
                var total = (owned?.Count ?? 0) + returnedItem.Count;

                db.Database.CreateExecutionStrategy().ExecuteInTransaction(() =>
                {
                    db.ChangeTracker.Clear();
                    var deleted = db.ClanShopItems
                        .Where(x => x.Id == shopItem.Id && x.ClanId == clanId && x.OwnerCharId == character.Id)
                        .ExecuteDelete();
                    if (deleted != 1)
                        throw new InvalidException("Vật phẩm không có trong kho tông môn");
                }, () =>
                    !db.ClanShopItems.AsNoTracking().Any(x => x.Id == shopItem.Id && x.ClanId == clanId));

                inventory.Add(returnedItem);
                inventoryItem = new ItemRef(returnedItem.Id, returnedItem.Type, total);
            }

            clan.ShopItems.RemoveAll(x => x.Id == shopItem.Id);
            NotifyClanShopItemRemoved(clanId, shopItem.Id);
            return new RemoveClanShopItemResult
            {
                Success = true,
                ShopItemId = shopItem.Id,
                InventoryItem = inventoryItem
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
            ValidateShopItem(reward, offer.Price);
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
            await using var db = await Program.CreateDbAsync();
            BuyClanShopResult result;
            var removeOffer = offer.Count == request.Quantity;
            lock (inventory.SyncRoot)
            {
                var collection = (HashSet<ItemRef>)inventory.GetItemCollection(reward.Type).Collection;
                collection.TryGetValue(reward, out var owned);
                if ((long)(owned?.Count ?? 0) + reward.Count > int.MaxValue)
                    throw new InvalidException("Số lượng vật phẩm trong túi vượt giới hạn");
                var total = (owned?.Count ?? 0) + reward.Count;
                var points = member.ContribPoints - cost;
                var newShopCount = offer.Count - request.Quantity;
                db.Database.CreateExecutionStrategy().ExecuteInTransaction(() =>
                {
                    db.ChangeTracker.Clear();
                    var updated = db.ClanMembers
                        .Where(x => x.ClanId == clanId && x.CharId == character.Id &&
                                    x.ContribPoints == member.ContribPoints && x.ContribPoints >= cost)
                        .ExecuteUpdate(x => x.SetProperty(m => m.ContribPoints, m => m.ContribPoints - cost));
                    if (updated != 1)
                        throw new InvalidException("Không đủ điểm cống hiến hoặc đã rời tông môn");

                    updated = db.ClanShopItems
                        .Where(x => x.Id == offer.Id && x.ClanId == clanId && x.Count == offer.Count && x.Count >= request.Quantity)
                        .ExecuteUpdate(x => x.SetProperty(s => s.Count, s => s.Count - request.Quantity));
                    if (updated != 1)
                        throw new InvalidException("Kho tông môn không đủ số lượng vật phẩm");
                    if (newShopCount == 0)
                    {
                        db.ClanShopItems
                            .Where(x => x.Id == offer.Id && x.ClanId == clanId && x.Count == 0)
                            .ExecuteDelete();
                    }
                }, () =>
                    db.ClanMembers.AsNoTracking().Any(x => x.ClanId == clanId &&
                        x.CharId == character.Id && x.ContribPoints == points) &&
                    (newShopCount == 0
                        ? !db.ClanShopItems.AsNoTracking().Any(x => x.Id == offer.Id && x.ClanId == clanId)
                        : db.ClanShopItems.AsNoTracking().Any(x => x.Id == offer.Id && x.ClanId == clanId && x.Count == newShopCount)));

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

                result = new BuyClanShopResult
                {
                    Success = true, ShopItemId = request.ShopItemId, Quantity = request.Quantity,
                    Cost = cost, ContribPoints = points,
                    InventoryItem = new ItemRef(reward.Id, reward.Type, total)
                };
            }

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

    private static void ValidateShopItem(ItemRef item, long price)
    {
        if (price <= 0 || item.Count <= 0 ||
            item.Type is not (ItemType.Herb or ItemType.LinhDan or ItemType.ThanThong
                or ItemType.CongPhap or ItemType.Recipe or ItemType.Material))
            throw new InvalidException("Cấu hình vật phẩm kho tông môn không hợp lệ");
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
