using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.LuyenKhi;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.Clan;

public static partial class ClanManager
{
    private sealed record InheritanceTransactionResult(
        ClanInheritanceEffect? Effect,
        long ContribPoints,
        int Cost,
        bool Changed,
        int MoneyVipCost = 0,
        int? NewMoneyVip = null,
        bool RequiresConfirmation = false);

    private sealed record UpgradeClanWithFundsTransactionResult(
        long FundsSpent,
        long AddedExp,
        long ClanFunds,
        long Exp,
        int Level,
        int MaxMembers);

    private sealed record LuyenHonTransactionResult(
        long AddedHon,
        long SoulEssenceSpent,
        long Hon,
        long MaxHon,
        int HonLimitLevel,
        long SoulEssence,
        long MaxSoulEssence);

    private sealed record UpgradeMaxHonTransactionResult(
        long ContribPointsCost,
        long AddedMaxHon,
        long ContribPoints,
        long Hon,
        long MaxHon,
        int HonLimitLevel,
        long SoulEssence,
        long MaxSoulEssence,
        long MoneyVipCost,
        int NewMoneyVip,
        bool RequiresConfirmation = false);

    public static long GetMaxHon(int honLimitLevel)
    {
        var index = checked(honLimitLevel - 1);
        if (index < 0 || index >= Config.MaxHonByLimitLevel.Length)
            throw new InvalidException("Cấu hình giới hạn hồn không hợp lệ");

        var maxHon = Config.MaxHonByLimitLevel[index];
        if (maxHon <= 0)
            throw new InvalidException("Cấu hình giới hạn hồn không hợp lệ");

        return maxHon;
    }

    public static long GetMaxSoulEssence(int honLimitLevel)
    {
        var index = checked(honLimitLevel - 1);
        if (index < 0 || index >= Config.MaxSoulEssenceByLimitLevel.Length)
            throw new InvalidException("Cấu hình giới hạn Hồn Tinh không hợp lệ");

        var maxSoulEssence = Config.MaxSoulEssenceByLimitLevel[index];
        if (maxSoulEssence < 0)
            throw new InvalidException("Cấu hình giới hạn Hồn Tinh không hợp lệ");

        return maxSoulEssence;
    }

    // Chi phí Hồn Tinh để luyện điểm Hồn thứ honPoint (đếm từ 1, honPoint = Hồn hiện tại + 1).
    // Chi phí không cố định: cứ mỗi SoulEssenceHonCostTierSize điểm Hồn thì tăng thêm 1 bậc,
    // mỗi bậc cộng thêm SoulEssenceHonCostGrowthPerTier so với bậc trước.
    // Ví dụ Base=1000, TierSize=50, GrowthPerTier=1000: điểm Hồn 1-50 tốn 1000, 51-100 tốn 2000,
    // 101-150 tốn 3000...
    public static long GetSoulEssenceCostForHonPoint(long honPoint)
    {
        if (honPoint <= 0)
            throw new InvalidException("Điểm Hồn không hợp lệ");
        if (Config.SoulEssenceHonCostTierSize <= 0 || Config.SoulEssenceHonCostBase < 0 ||
            Config.SoulEssenceHonCostGrowthPerTier < 0)
            throw new InvalidException("Cấu hình chi phí luyện hồn không hợp lệ");

        var tier = (honPoint - 1) / Config.SoulEssenceHonCostTierSize;
        try
        {
            return checked(Config.SoulEssenceHonCostBase + tier * Config.SoulEssenceHonCostGrowthPerTier);
        }
        catch (OverflowException)
        {
            throw new InvalidException("Chi phí luyện hồn vượt giới hạn");
        }
    }

    public static long GetHonLimitUpgradeCost(int honLimitLevel)
    {
        var index = checked(honLimitLevel - 1);
        if (index <= 0 || index >= Config.UpgradeHonLimitFundCostByLevel.Length)
            throw new InvalidException("Cấu hình giá mở giới hạn hồn không hợp lệ");

        var cost = Config.UpgradeHonLimitFundCostByLevel[index];
        if (cost < 0)
            throw new InvalidException("Cấu hình giá mở giới hạn hồn không hợp lệ");

        return cost;
    }

    public static int GetInheritanceSlots(int clanLevel)
    {
        if (Config.BaseInheritanceSlots < 0 || Config.InheritanceSlotsPerLevel < 0 ||
            Config.MaxInheritanceSlotsPerColumn < 0)
            throw new InvalidException("Cấu hình số ô truyền thừa không hợp lệ");

        var slots = (long)Config.BaseInheritanceSlots +
                    (long)Math.Max(0, clanLevel - 1) * Config.InheritanceSlotsPerLevel;
        return (int)Math.Clamp(slots, 0, Config.MaxInheritanceSlotsPerColumn);
    }

    public static int GetInheritanceValue(EffectType? effectType, int clanLevel = 1)
    {
        if (Config.InheritanceBaseValue < 0)
            throw new InvalidException("Cấu hình giá trị truyền thừa không hợp lệ");
        try
        {
            var (min,max) = GameConfig.ClanBuffResources.GetBuffValueRangeOf(effectType, clanLevel);
            return (int)Random.Shared.NextInt64(min, (long)max + 1);
        }
        catch (OverflowException)
        {
            throw new InvalidException("Giá trị truyền thừa vượt giới hạn");
        }
    }

    private static async Task ValidateGiamTonThuongTotalLimit(
        AppDbContext db,
        int clanId,
        ClanInheritanceTrigger trigger,
        int slotIndex,
        EffectType? effectType,
        int value)
    {
        var otherTotal = await db.Set<ClanInheritanceEffect>()
            .Where(x => x.ClanId == clanId &&
                        x.EffectType == EffectType.GiamTonThuong &&
                        (x.Trigger != trigger || x.SlotIndex != slotIndex))
            .SumAsync(x => (int?)x.Value) ?? 0;
        var total = otherTotal + (effectType == EffectType.GiamTonThuong ? value : 0);

        if (total > 50)
        {
            throw new InvalidException("Tổng truyền thừa Giảm Tổn Thương tối đa 50 điểm");
        }
    }

    public static async Task<SetClanInheritanceResult> SetInheritance(
        ClanReference reference,
        CharacterData actor,
        SetClanInheritanceRequest request)
    {
        if (!Enum.IsDefined(request.Trigger))
            throw new InvalidException("Cột truyền thừa không hợp lệ");

        var clan = reference.Clan;
        var slotCount = GetInheritanceSlots(clan.Level);
        if (request.SlotIndex < 0 || request.SlotIndex >= slotCount)
            throw new InvalidException("Ô truyền thừa chưa được mở khóa");
        if (request.EffectType != null &&
            !Config.InheritanceBuffWhitelist.Contains(request.EffectType.Value))
            throw new InvalidException("Hiệu ứng không nằm trong danh sách truyền thừa cho phép");

        var actorMember = clan.Members.FirstOrDefault(x => x.CharId == actor.Id)
                          ?? throw new InvalidException("Chưa gia nhập tông môn");
        if (!actorMember.HasPermission(ClanPermission.ManageInheritance))
            throw new InvalidException("Không có quyền quản lý truyền thừa");

        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var current = clan.InheritanceEffects.FirstOrDefault(x =>
                x.Trigger == request.Trigger && x.SlotIndex == request.SlotIndex);
            var ramChanged = request.EffectType == null
                ? current != null
                : current == null || current.EffectType != request.EffectType;
            var value = request.EffectType.HasValue ? GetInheritanceValue(request.EffectType, clan.Level) : 0;
            InheritanceTransactionResult? attemptedChange = null;
            var transactionResult = await ExecuteClanTransactionAsync(async db =>
            {
                var persisted = await db.Set<ClanInheritanceEffect>().FirstOrDefaultAsync(x =>
                    x.ClanId == clanId && x.Trigger == request.Trigger && x.SlotIndex == request.SlotIndex);

                var alreadyDesired = request.EffectType == null
                    ? persisted == null
                    : persisted?.EffectType == request.EffectType;
                if (alreadyDesired)
                {
                    var unchangedPoints = await db.ClanMembers
                        .Where(x => x.ClanId == clanId && x.CharId == actor.Id)
                        .Select(x => x.ContribPoints).SingleAsync();
                    // A transient commit failure can retry after the payment was already committed.
                    if (attemptedChange != null)
                        return attemptedChange with { Effect = persisted, ContribPoints = unchangedPoints };
                    return new InheritanceTransactionResult(persisted, unchangedPoints, 0, false);
                }

                await ValidateGiamTonThuongTotalLimit(
                    db,
                    clanId,
                    request.Trigger,
                    request.SlotIndex,
                    request.EffectType,
                    value);

                var totalCost = persisted != null ? Config.ChangeInheritanceContribPointsCost : 0;
                if (totalCost < 0)
                    throw new InvalidException("Cấu hình phí thay đổi truyền thừa không hợp lệ");

                var currentPoints = await db.ClanMembers
                    .Where(x => x.ClanId == clanId && x.CharId == actor.Id)
                    .Select(x => x.ContribPoints).SingleAsync();
                var cost = (int)Math.Min(Math.Max(0, currentPoints), totalCost);
                var shortfall = totalCost - cost;
                var moneyVipCost = shortfall / 2 + shortfall % 2;
                if (request.ExpectedContribPointsCost != cost || request.ExpectedMoneyVipCost != moneyVipCost)
                    return new InheritanceTransactionResult(persisted, currentPoints, cost, false,
                        moneyVipCost, RequiresConfirmation: true);

                var newPoints = currentPoints - cost;
                var newMoneyVip = actor.MoneyVip - moneyVipCost;
                if (newMoneyVip < 0)
                    throw new InvalidException("Không đủ Tiên Ngọc để bù phần điểm cống hiến còn thiếu");

                if (moneyVipCost > 0)
                {
                    var updatedActor = await db.Characters
                        .Where(x => x.Id == actor.Id && x.MoneyVip == actor.MoneyVip && x.MoneyVip >= moneyVipCost)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.MoneyVip, newMoneyVip));
                    if (updatedActor != 1)
                        throw new InvalidException("Tiên Ngọc đã thay đổi, vui lòng thử lại");
                    await db.SpendingMoneyVips.AddAsync(new SpendingMoneyVip
                    {
                        CharId = actor.Id,
                        SpendFor = SpendFor.ChangeClanInheritance,
                        MoneyVip = moneyVipCost,
                        Time = DateTime.UtcNow
                    });
                }

                if (cost > 0)
                {
                    var updated = await db.ClanMembers
                        .Where(x => x.ClanId == clanId && x.CharId == actor.Id &&
                                    x.ContribPoints == currentPoints && x.ContribPoints >= cost)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.ContribPoints, newPoints));
                    if (updated != 1)
                        throw new InvalidException("Điểm cống hiến đã thay đổi, vui lòng thử lại");
                }

                if (request.EffectType == null)
                {
                    if (persisted != null) db.Remove(persisted);
                }
                else if (persisted == null)
                {
                    persisted = new ClanInheritanceEffect
                    {
                        ClanId = clanId,
                        Trigger = request.Trigger,
                        SlotIndex = request.SlotIndex,
                        EffectType = request.EffectType,
                        Value = value
                    };
                    await db.AddAsync(persisted);
                }
                else
                {
                    persisted.EffectType = request.EffectType;
                    persisted.Value = value;
                }

                await db.SaveChangesAsync();
                attemptedChange = new InheritanceTransactionResult(
                    request.EffectType == null ? null : persisted,
                    newPoints,
                    cost,
                    true,
                    moneyVipCost,
                    newMoneyVip);
                return attemptedChange;
            });

            actorMember.ContribPoints = transactionResult.ContribPoints;
            if (transactionResult.RequiresConfirmation)
                return new SetClanInheritanceResult
                {
                    Success = false,
                    RequiresConfirmation = true,
                    Message = "Chi phí đã thay đổi, vui lòng xác nhận lại",
                    ClanFunds = clan.Funds,
                    ContribPoints = transactionResult.ContribPoints,
                    Cost = transactionResult.Cost,
                    MoneyVipCost = transactionResult.MoneyVipCost,
                    NewMoneyVip = actor.MoneyVip
                };

            if (transactionResult.NewMoneyVip.HasValue)
                actor.SetMoneyVip(transactionResult.NewMoneyVip.Value);
            clan.InheritanceEffects.RemoveAll(x =>
                x.Trigger == request.Trigger && x.SlotIndex == request.SlotIndex);
            if (transactionResult.Effect != null)
            {
                transactionResult.Effect.Clan = clan;
                clan.InheritanceEffects.Add(transactionResult.Effect);
            }

            clan.RefreshInheritanceViews();

            var change = new ClanInheritanceChanged
            {
                ClanId = clanId,
                Trigger = request.Trigger,
                SlotIndex = request.SlotIndex,
                Effect = transactionResult.Effect
            };
            if (ramChanged && Observers.TryGetValue(clanId, out var observer))
            {
                observer.SendAll(MessageTag.ClanInheritanceChanged, change);
            }
            if (transactionResult.Cost > 0)
                NotifyClanMemberPointsChanged(clanId, actor.Id, transactionResult.ContribPoints);

            return new SetClanInheritanceResult
            {
                Success = true,
                Change = change,
                NewMoneyVip = actor.MoneyVip,
                ClanFunds = clan.Funds,
                ContribPoints = transactionResult.ContribPoints,
                Cost = transactionResult.Cost,
                MoneyVipCost = transactionResult.MoneyVipCost
            };
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    public static async Task<UpgradeClanWithFundsResult> UpgradeWithFunds(
        ClanReference reference,
        CharacterData actor,
        long funds)
    {
        if (funds <= 0)
            throw new InvalidException("Số quỹ nâng cấp phải lớn hơn 0");

        var clan = reference.Clan;
        var actorMember = clan.Members.FirstOrDefault(x => x.CharId == actor.Id)
                          ?? throw new InvalidException("Chưa gia nhập tông môn");
        if (actorMember.Role != ClanRole.Leader && clan.LeaderCharId != actor.Id)
            throw new InvalidException("Chỉ Tộc Chủ được nâng cấp tông môn");
        if (clan.Funds < funds)
            throw new InvalidException("Quỹ tông môn không đủ");

        var addedExp = GetClanUpgradeExpFromFunds(funds);
        if (addedExp <= 0)
            throw new InvalidException("EXP nâng cấp phải lớn hơn 0");

        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var transactionResult = await ExecuteClanTransactionAsync(async db =>
            {
                var persisted = await db.Clans
                    .Where(x => x.Id == clanId)
                    .Select(x => new { x.Funds, x.Exp, x.Level, x.MaxMembers })
                    .SingleAsync();
                if (persisted.Funds < funds)
                    throw new InvalidException("Quỹ tông môn không đủ");

                long newFunds;
                long newExp;
                try
                {
                    newFunds = checked(persisted.Funds - funds);
                    newExp = checked(persisted.Exp + addedExp);
                }
                catch (OverflowException)
                {
                    throw new InvalidException("Tài nguyên tông môn đã đạt giới hạn");
                }

                var (newLevel, remainingExp) = ResolveClanLevelProgress(
                    persisted.Level,
                    newExp);
                var newMaxMembers = GetMaxMembersAfterLevelUp(
                    persisted.MaxMembers,
                    persisted.Level,
                    newLevel);
                var updated = await db.Clans
                    .Where(x => x.Id == clanId &&
                                x.Funds == persisted.Funds &&
                                x.Exp == persisted.Exp &&
                                x.Level == persisted.Level)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Funds, newFunds)
                        .SetProperty(c => c.Exp, remainingExp)
                        .SetProperty(c => c.Level, newLevel)
                        .SetProperty(c => c.MaxMembers, newMaxMembers));
                if (updated != 1)
                    throw new InvalidException("Dữ liệu tông môn đã thay đổi, vui lòng thử lại");

                return new UpgradeClanWithFundsTransactionResult(
                    funds,
                    addedExp,
                    newFunds,
                    remainingExp,
                    newLevel,
                    newMaxMembers);
            });

            var levelChanged = clan.Level != transactionResult.Level;
            clan.Funds = transactionResult.ClanFunds;
            clan.Exp = transactionResult.Exp;
            clan.Level = transactionResult.Level;
            clan.MaxMembers = transactionResult.MaxMembers;
            if (levelChanged) clan.RefreshInheritanceViews();

            var effects = clan.InheritanceEffects.ToArray();
            if (Observers.TryGetValue(clanId, out var observer))
            {
                observer.SendAll(MessageTag.ClanFundsChanged,
                    new ClanFundsChanged { ClanId = clanId, Funds = transactionResult.ClanFunds });
                observer.SendAll(MessageTag.ClanExpChanged,
                    new ClanExpChanged { ClanId = clanId, Exp = transactionResult.Exp });
                if (levelChanged)
                {
                    observer.SendAll(MessageTag.ClanLevelChanged, new ClanLevelChanged
                    {
                        ClanId = clanId,
                        Level = transactionResult.Level,
                        Exp = transactionResult.Exp,
                        MaxMembers = transactionResult.MaxMembers,
                        InheritanceSlotsPerColumn = GetInheritanceSlots(transactionResult.Level),
                        Effects = effects
                    });
                }
            }

            return new UpgradeClanWithFundsResult
            {
                Success = true,
                FundsSpent = transactionResult.FundsSpent,
                AddedExp = transactionResult.AddedExp,
                ClanFunds = transactionResult.ClanFunds,
                Exp = transactionResult.Exp,
                Level = transactionResult.Level,
                MaxMembers = transactionResult.MaxMembers,
                InheritanceSlotsPerColumn = GetInheritanceSlots(transactionResult.Level),
                Effects = effects
            };
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    public static long GetClanUpgradeExpFromFunds(long funds)
    {
        return funds;
    }

    public static async Task<LuyenHonResult> LuyenHon(
        ClanReference reference,
        CharacterData actor,
        long amount)
    {
        if (amount <= 0)
            throw new InvalidException("Số hồn luyện phải lớn hơn 0");

        var clan = reference.Clan;
        var actorMember = clan.Members.FirstOrDefault(x => x.CharId == actor.Id)
                          ?? throw new InvalidException("Chưa gia nhập tông môn");
        if (!actorMember.HasPermission(ClanPermission.ManageHon))
            throw new InvalidException("Không có quyền luyện hồn tông môn");
        if (clan.Hon >= clan.MaxHon)
            throw new InvalidException("Hồn tông môn đã đạt giới hạn");

        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var transactionResult = await ExecuteClanTransactionAsync(async db =>
            {
                var persisted = await db.Clans
                    .Where(x => x.Id == clanId)
                    .Select(x => new { x.Hon, x.MaxHon, x.HonLimitLevel, x.SoulEssence })
                    .SingleAsync();
                if (persisted.Hon >= persisted.MaxHon)
                    throw new InvalidException("Hồn tông môn đã đạt giới hạn");

                // Chi phí mỗi điểm Hồn tăng dần theo bậc (GetSoulEssenceCostForHonPoint) nên không
                // thể chia đều số Hồn Tinh hiện có cho một đơn giá cố định; mua từng điểm Hồn một
                // cho tới khi hết Hồn Tinh, hết chỗ trống (MaxHon) hoặc đủ số lượng yêu cầu.
                long newHon;
                long addedHon = 0;
                long soulEssenceSpent = 0;
                try
                {
                    var space = checked(persisted.MaxHon - persisted.Hon);
                    var maxAddable = Math.Min(amount, space);
                    var remainingSoulEssence = persisted.SoulEssence;
                    while (addedHon < maxAddable)
                    {
                        var nextHonPoint = checked(persisted.Hon + addedHon + 1);
                        var cost = GetSoulEssenceCostForHonPoint(nextHonPoint);
                        if (remainingSoulEssence < cost) break;

                        remainingSoulEssence -= cost;
                        soulEssenceSpent = checked(soulEssenceSpent + cost);
                        addedHon++;
                    }
                    newHon = checked(persisted.Hon + addedHon);
                }
                catch (OverflowException)
                {
                    throw new InvalidException("Hồn tông môn đã đạt giới hạn");
                }
                if (addedHon <= 0)
                    throw new InvalidException("Hồn Tinh tông môn không đủ");

                var newSoulEssence = persisted.SoulEssence - soulEssenceSpent;
                var maxSoulEssence = GetMaxSoulEssence(persisted.HonLimitLevel);

                var updated = await db.Clans
                    .Where(x => x.Id == clanId &&
                                x.Hon == persisted.Hon &&
                                x.MaxHon == persisted.MaxHon &&
                                x.SoulEssence == persisted.SoulEssence)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Hon, newHon)
                        .SetProperty(c => c.SoulEssence, newSoulEssence));
                if (updated != 1)
                    throw new InvalidException("Dữ liệu hồn tông môn đã thay đổi, vui lòng thử lại");

                return new LuyenHonTransactionResult(
                    addedHon,
                    soulEssenceSpent,
                    newHon,
                    persisted.MaxHon,
                    persisted.HonLimitLevel,
                    newSoulEssence,
                    maxSoulEssence);
            });

            clan.Hon = transactionResult.Hon;
            clan.MaxHon = transactionResult.MaxHon;
            clan.HonLimitLevel = transactionResult.HonLimitLevel;
            clan.SoulEssence = transactionResult.SoulEssence;
            NotifyClanHonChanged(clan);

            return new LuyenHonResult
            {
                Success = true,
                AddedHon = transactionResult.AddedHon,
                SoulEssenceSpent = transactionResult.SoulEssenceSpent,
                Hon = transactionResult.Hon,
                MaxHon = transactionResult.MaxHon,
                HonLimitLevel = transactionResult.HonLimitLevel,
                SoulEssence = transactionResult.SoulEssence,
                MaxSoulEssence = transactionResult.MaxSoulEssence
            };
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    public static async Task<UpgradeClanMaxHonResult> UpgradeMaxHon(
        ClanReference reference,
        CharacterData actor,
        UpgradeClanMaxHonRequest request)
    {
        var clan = reference.Clan;
        var actorMember = clan.Members.FirstOrDefault(x => x.CharId == actor.Id)
                          ?? throw new InvalidException("Chưa gia nhập tông môn");
        if (!actorMember.HasPermission(ClanPermission.ManageHon))
            throw new InvalidException("Không có quyền nâng giới hạn hồn tông môn");

        var nextLimitLevel = clan.HonLimitLevel + 1;
        var limitLevel = request.LimitLevel <= 0 ? nextLimitLevel : request.LimitLevel;
        if (limitLevel != nextLimitLevel)
            throw new InvalidException("Chỉ có thể mở giới hạn hồn từng cấp");

        var newMaxHon = GetMaxHon(limitLevel);
        var newMaxSoulEssence = GetMaxSoulEssence(limitLevel);
        var totalCost = GetHonLimitUpgradeCost(limitLevel);
        if (newMaxHon <= clan.MaxHon)
            throw new InvalidException("Cấu hình giới hạn hồn không hợp lệ");

        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            UpgradeMaxHonTransactionResult? attemptedUpgrade = null;
            var transactionResult = await ExecuteClanTransactionAsync(async db =>
            {
                var persisted = await db.Clans
                    .Where(x => x.Id == clanId)
                    .Select(x => new { x.Hon, x.MaxHon, x.HonLimitLevel, x.SoulEssence })
                    .SingleAsync();
                if (attemptedUpgrade != null && persisted.HonLimitLevel == limitLevel)
                    return attemptedUpgrade;
                var persistedNextLimitLevel = persisted.HonLimitLevel + 1;
                if (limitLevel != persistedNextLimitLevel)
                    throw new InvalidException("Chỉ có thể mở giới hạn hồn từng cấp");
                var currentPoints = await db.ClanMembers
                    .Where(x => x.ClanId == clanId && x.CharId == actor.Id)
                    .Select(x => x.ContribPoints).SingleAsync();
                var pointsCost = Math.Min(Math.Max(0, currentPoints), totalCost);
                var shortfall = totalCost - pointsCost;
                var moneyVipCost = shortfall / 2 + shortfall % 2;
                if (request.ExpectedContribPointsCost != pointsCost || request.ExpectedMoneyVipCost != moneyVipCost)
                    return new UpgradeMaxHonTransactionResult(pointsCost, 0, currentPoints,
                        persisted.Hon, persisted.MaxHon, persisted.HonLimitLevel, persisted.SoulEssence,
                        GetMaxSoulEssence(persisted.HonLimitLevel), moneyVipCost, actor.MoneyVip, true);
                if (moneyVipCost > actor.MoneyVip)
                    throw new InvalidException("Không đủ Tiên Ngọc để bù điểm cống hiến còn thiếu");
                var newMoneyVip = actor.MoneyVip - (int)moneyVipCost;

                var newPoints = currentPoints - pointsCost;
                long addedMaxHon;
                try
                {
                    addedMaxHon = checked(newMaxHon - persisted.MaxHon);
                }
                catch (OverflowException)
                {
                    throw new InvalidException("Tài nguyên hồn tông môn đã đạt giới hạn");
                }
                if (addedMaxHon <= 0)
                    throw new InvalidException("Cấu hình giới hạn hồn không hợp lệ");

                if (pointsCost > 0)
                {
                    var updatedMember = await db.ClanMembers
                        .Where(x => x.ClanId == clanId && x.CharId == actor.Id && x.ContribPoints == currentPoints)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.ContribPoints, newPoints));
                    if (updatedMember != 1)
                        throw new InvalidException("Điểm cống hiến đã thay đổi, vui lòng thử lại");
                }
                if (moneyVipCost > 0)
                {
                    var updatedActor = await db.Characters
                        .Where(x => x.Id == actor.Id && x.MoneyVip == actor.MoneyVip && x.MoneyVip >= moneyVipCost)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.MoneyVip, newMoneyVip));
                    if (updatedActor != 1)
                        throw new InvalidException("Tiên Ngọc đã thay đổi, vui lòng thử lại");
                    await db.SpendingMoneyVips.AddAsync(new SpendingMoneyVip
                    {
                        CharId = actor.Id,
                        SpendFor = SpendFor.UpgradeClanHonLimit,
                        MoneyVip = moneyVipCost,
                        Time = DateTime.UtcNow
                    });
                }

                var updated = await db.Clans
                    .Where(x => x.Id == clanId &&
                                x.Hon == persisted.Hon &&
                                x.MaxHon == persisted.MaxHon &&
                                x.HonLimitLevel == persisted.HonLimitLevel)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.MaxHon, newMaxHon)
                        .SetProperty(c => c.HonLimitLevel, limitLevel));
                if (updated != 1)
                    throw new InvalidException("Dữ liệu tông môn đã thay đổi, vui lòng thử lại");

                await db.SaveChangesAsync();
                attemptedUpgrade = new UpgradeMaxHonTransactionResult(
                    pointsCost,
                    addedMaxHon,
                    newPoints,
                    persisted.Hon,
                    newMaxHon,
                    limitLevel,
                    persisted.SoulEssence,
                    newMaxSoulEssence,
                    moneyVipCost,
                    newMoneyVip);
                return attemptedUpgrade;
            });

            actorMember.ContribPoints = transactionResult.ContribPoints;
            if (transactionResult.RequiresConfirmation)
                return new UpgradeClanMaxHonResult
                {
                    RequiresConfirmation = true,
                    Message = "Chi phí đã thay đổi, vui lòng xác nhận lại",
                    ContribPointsCost = transactionResult.ContribPointsCost,
                    ContribPoints = transactionResult.ContribPoints,
                    MoneyVipCost = transactionResult.MoneyVipCost,
                    NewMoneyVip = actor.MoneyVip
                };
            actor.SetMoneyVip(transactionResult.NewMoneyVip);
            clan.Hon = transactionResult.Hon;
            clan.MaxHon = transactionResult.MaxHon;
            clan.HonLimitLevel = transactionResult.HonLimitLevel;
            clan.SoulEssence = transactionResult.SoulEssence;
            if (Observers.TryGetValue(clanId, out var observer))
            {
                observer.SendAll(MessageTag.ClanHonChanged, CreateClanHonChanged(clan));
            }
            if (transactionResult.ContribPointsCost > 0)
                NotifyClanMemberPointsChanged(clanId, actor.Id, transactionResult.ContribPoints);

            return new UpgradeClanMaxHonResult
            {
                Success = true,
                ContribPointsCost = transactionResult.ContribPointsCost,
                ContribPoints = transactionResult.ContribPoints,
                MoneyVipCost = transactionResult.MoneyVipCost,
                NewMoneyVip = transactionResult.NewMoneyVip,
                AddedMaxHon = transactionResult.AddedMaxHon,
                ClanFunds = clan.Funds,
                Hon = transactionResult.Hon,
                MaxHon = transactionResult.MaxHon,
                HonLimitLevel = transactionResult.HonLimitLevel,
                SoulEssence = transactionResult.SoulEssence,
                MaxSoulEssence = transactionResult.MaxSoulEssence
            };
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    private static void NotifyClanHonChanged(Models.Clan clan)
    {
        var clanId = clan.Id;
        if (!Observers.TryGetValue(clanId, out var observer)) return;
        observer.SendAll(MessageTag.ClanHonChanged, CreateClanHonChanged(clan));
    }

    private static ClanHonChanged CreateClanHonChanged(Models.Clan clan)
    {
        return new ClanHonChanged
        {
            ClanId = clan.Id,
            Hon = clan.Hon,
            MaxHon = clan.MaxHon,
            HonLimitLevel = clan.HonLimitLevel,
            SoulEssence = clan.SoulEssence,
            MaxSoulEssence = GetMaxSoulEssence(clan.HonLimitLevel)
        };
    }

    // Chỉ dồn Hồn Tinh vào bộ đếm tạm trong RAM (không đụng DB/gate), để hàm này gọi được
    // trực tiếp trên luồng xử lý kết quả combat mỗi lần giết quái mà không gây độ trễ hay
    // tranh chấp gate với các mutation khác của tông môn (Funds, Shop, Inheritance...).
    // ClanSoulEssenceFlushService sẽ định kỳ gom và ghi xuống DB qua FlushPendingSoulEssence.
    public static void AddSoulEssenceFromMonsterKill(ClanReference reference)
    {
        QueuePendingSoulEssence(reference.Clan.Id, Config.MonsterKillSoulEssenceReward);
    }

    public static async Task TryAddSoulEssenceFromMonsterKill(int charId)
    {
        try
        {
            var reference = await FindReferenceByMember(charId);
            AddSoulEssenceFromMonsterKill(reference);
        }
        catch (InvalidException)
        {
        }
        catch (Exception ex)
        {
            // Gọi fire-and-forget từ luồng combat nên phải tự bắt hết lỗi ở đây,
            // tránh exception "mồ côi" không ai quan sát.
            Log.Error(ex, "Không thể cộng Hồn Tinh từ giết quái cho nhân vật {CharId}", charId);
        }
    }

    private static void QueuePendingSoulEssence(int clanId, long amount)
    {
        if (amount <= 0) return;
        PendingSoulEssence.AddOrUpdate(clanId, amount, (_, existing) => existing + amount);
    }

    // Được ClanSoulEssenceFlushService gọi định kỳ: lấy hết Hồn Tinh đang chờ của từng
    // tông môn đang có farm và ghi xuống DB thành một lần cộng duy nhất mỗi tông môn mỗi
    // chu kỳ, thay vì một lần ghi DB cho mỗi lượt giết quái.
    public static async Task FlushPendingSoulEssence()
    {
        if (PendingSoulEssence.IsEmpty) return;

        foreach (var clanId in PendingSoulEssence.Keys.ToArray())
        {
            if (!PendingSoulEssence.TryRemove(clanId, out var amount) || amount <= 0) continue;

            try
            {
                var reference = await FindReference(clanId);
                await AddSoulEssence(reference, amount);
            }
            catch (InvalidException ex)
            {
                // Tông môn không còn tồn tại hoặc dữ liệu đã đổi giữa chừng; bỏ qua phần
                // Hồn Tinh đang chờ của tông môn này thay vì làm hỏng cả lượt flush.
                Log.Warning(ex, "Không thể flush Hồn Tinh cho tông môn {ClanId}", clanId);
            }
        }
    }

    public static async Task AddSoulEssence(ClanReference reference, long amount)
    {
        if (amount <= 0) throw new InvalidException("Số Hồn Tinh cộng thêm phải lớn hơn 0");
        var clan = reference.Clan;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var transactionResult = await ExecuteClanTransactionAsync(async db =>
            {
                var persisted = await db.Clans
                    .Where(x => x.Id == clanId)
                    .Select(x => new { x.HonLimitLevel, x.SoulEssence })
                    .SingleAsync();
                var maxSoulEssence = GetMaxSoulEssence(persisted.HonLimitLevel);
                long newSoulEssence;
                try
                {
                    newSoulEssence = Math.Min(maxSoulEssence, checked(persisted.SoulEssence + amount));
                }
                catch (OverflowException)
                {
                    newSoulEssence = maxSoulEssence;
                }
                var updated = await db.Clans
                    .Where(x => x.Id == clanId && x.SoulEssence == persisted.SoulEssence)
                    .ExecuteUpdateAsync(x => x.SetProperty(c => c.SoulEssence, newSoulEssence));
                if (updated != 1)
                    throw new InvalidException("Dữ liệu Hồn Tinh tông môn đã thay đổi, vui lòng thử lại");

                return (persisted.HonLimitLevel, SoulEssence: newSoulEssence);
            });

            clan.HonLimitLevel = transactionResult.HonLimitLevel;
            clan.SoulEssence = transactionResult.SoulEssence;
            NotifyClanHonChanged(clan);
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    // Nguồn EXP sẽ gọi hàm này sau. Hiện chưa nối vào cống hiến hay gameplay nào.
    public static async Task AddExp(ClanReference reference, long amount)
    {
        if (amount <= 0) throw new InvalidException("Clan EXP cộng thêm phải lớn hơn 0");
        var clan = reference.Clan;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            long newExp;
            try
            {
                newExp = checked(clan.Exp + amount);
            }
            catch (OverflowException)
            {
                throw new InvalidException("Clan EXP đã đạt giới hạn");
            }

            var (newLevel, remainingExp) = ResolveClanLevelProgress(clan.Level, newExp);
            var newMaxMembers = GetMaxMembersAfterLevelUp(clan.MaxMembers, clan.Level, newLevel);
            await ExecuteClanTransactionAsync(async db =>
            {
                var updated = await db.Clans
                    .Where(x => x.Id == clanId && x.Exp == clan.Exp && x.Level == clan.Level)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Exp, remainingExp)
                        .SetProperty(c => c.Level, newLevel)
                        .SetProperty(c => c.MaxMembers, newMaxMembers));
                if (updated != 1 && !await db.Clans.AnyAsync(x =>
                        x.Id == clanId && x.Exp == remainingExp && x.Level == newLevel &&
                        x.MaxMembers == newMaxMembers))
                    throw new InvalidException("Dữ liệu cấp tông môn đã thay đổi, vui lòng thử lại");

                return true;
            });

            var levelChanged = clan.Level != newLevel;
            clan.Exp = remainingExp;
            clan.Level = newLevel;
            clan.MaxMembers = newMaxMembers;
            if (levelChanged)
            {
                clan.RefreshInheritanceViews();
            }

            if (Observers.TryGetValue(clanId, out var observer))
            {
                observer.SendAll(MessageTag.ClanExpChanged,
                    new ClanExpChanged { ClanId = clanId, Exp = remainingExp });
                if (levelChanged)
                {
                    observer.SendAll(MessageTag.ClanLevelChanged, new ClanLevelChanged
                    {
                        ClanId = clanId,
                        Level = newLevel,
                        Exp = remainingExp,
                        MaxMembers = newMaxMembers,
                        InheritanceSlotsPerColumn = GetInheritanceSlots(newLevel),
                        Effects = clan.InheritanceEffects.ToArray()
                    });
                }
            }
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    private static (int Level, long Exp) ResolveClanLevelProgress(int currentLevel, long currentExp)
    {
        var milestones = Config.ClanLevelRequiredTotalExp;
        if (milestones.Length == 0) return (Math.Max(1, currentLevel), currentExp);
        if (milestones[0] != 0)
            throw new InvalidException("Mốc EXP cấp 1 của tông môn phải bằng 0");

        for (var index = 0; index < milestones.Length; index++)
        {
            if (milestones[index] < 0 || index > 0 && milestones[index] <= milestones[index - 1])
                throw new InvalidException("Các mốc EXP tông môn phải tăng dần và không âm");
        }

        var level = Math.Clamp(currentLevel, 1, milestones.Length);
        var exp = currentExp;
        while (level < milestones.Length)
        {
            var required = milestones[level] - milestones[level - 1];
            if (exp < required) break;

            exp -= required;
            level++;
        }

        return (level, exp);
    }

    private static int GetMaxMembersAfterLevelUp(int currentMaxMembers, int currentLevel, int newLevel)
    {
        var levelInterval = Math.Max(1, Config.MemberSlotLevelInterval);
        var slotsPerInterval = Math.Max(0, Config.MemberSlotsPerInterval);
        var oldMilestone = Math.Max(0, currentLevel - 1) / levelInterval;
        var newMilestone = Math.Max(0, newLevel - 1) / levelInterval;
        var milestonesPassed = Math.Max(0, newMilestone - oldMilestone);
        try
        {
            return checked(currentMaxMembers + milestonesPassed * slotsPerInterval);
        }
        catch (OverflowException)
        {
            throw new InvalidException("Số slot thành viên tông môn đã đạt giới hạn");
        }
    }
}
