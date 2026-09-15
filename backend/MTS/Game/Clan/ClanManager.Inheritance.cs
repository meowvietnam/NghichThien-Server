using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Game.LuyenKhi;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.Clan;

public static partial class ClanManager
{
    private sealed record InheritanceTransactionResult(
        ClanInheritanceEffect? Effect, long ClanFunds, int Cost, bool Changed);

    private sealed record UpgradeClanWithFundsTransactionResult(
        long FundsSpent,
        long AddedExp,
        long ClanFunds,
        long Exp,
        int Level,
        int MaxMembers);

    public static int GetInheritanceSlots(int clanLevel)
    {
        if (Config.BaseInheritanceSlots < 0 || Config.InheritanceSlotsPerLevel < 0 ||
            Config.MaxInheritanceSlotsPerColumn < 0)
            throw new InvalidException("Cấu hình số ô truyền thừa không hợp lệ");

        var slots = (long)Config.BaseInheritanceSlots +
                    (long)Math.Max(0, clanLevel - 1) * Config.InheritanceSlotsPerLevel;
        return (int)Math.Clamp(slots, 0, Config.MaxInheritanceSlotsPerColumn);
    }

    public static int GetInheritanceValue(EffectType? effectType)
    {
        if (Config.InheritanceBaseValue < 0)
            throw new InvalidException("Cấu hình giá trị truyền thừa không hợp lệ");

        try
        {
            var baseValue = effectType.HasValue
                ? GameConfig.ClanBuffResources.GetBaseValueOf(effectType.Value)
                : Config.InheritanceBaseValue;
            if (baseValue < 0)
                throw new InvalidException("Cấu hình giá trị truyền thừa không hợp lệ");
            var minValue = baseValue / 2;
            var maxValue = checked(baseValue * 2);
            return Random.Shared.Next(minValue, maxValue + 1);
        }
        catch (OverflowException)
        {
            throw new InvalidException("Giá trị truyền thừa vượt giới hạn");
        }
    }

    private static int GetLeveledInheritanceValue(EffectType? effectType, int clanLevel)
    {
        var value = GetInheritanceValue(effectType);
        try
        {
            return checked(value * Math.Max(1, clanLevel));
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
            var changingFilledSlot = current != null && current.EffectType != request.EffectType;
            var expectedCost = changingFilledSlot ? Config.ChangeInheritanceMoneyVipCost : 0;
            if (expectedCost < 0)
                throw new InvalidException("Cấu hình phí thay đổi truyền thừa không hợp lệ");
            if (clan.Funds < expectedCost)
                throw new InvalidException("Quỹ tông môn không đủ để thay đổi truyền thừa");

            var value = request.EffectType.HasValue ? GetLeveledInheritanceValue(request.EffectType, clan.Level) : 0;
            var transactionResult = await ExecuteClanTransactionAsync(async db =>
            {
                var persisted = await db.Set<ClanInheritanceEffect>().FirstOrDefaultAsync(x =>
                    x.ClanId == clanId && x.Trigger == request.Trigger && x.SlotIndex == request.SlotIndex);

                var alreadyDesired = request.EffectType == null
                    ? persisted == null
                    : persisted?.EffectType == request.EffectType;
                if (alreadyDesired)
                {
                    var unchangedFunds = await db.Clans.Where(x => x.Id == clanId)
                        .Select(x => x.Funds).SingleAsync();
                    return new InheritanceTransactionResult(persisted, unchangedFunds, 0, false);
                }

                await ValidateGiamTonThuongTotalLimit(
                    db,
                    clanId,
                    request.Trigger,
                    request.SlotIndex,
                    request.EffectType,
                    value);

                var cost = persisted != null ? Config.ChangeInheritanceMoneyVipCost : 0;
                if (cost < 0)
                    throw new InvalidException("Cấu hình phí thay đổi truyền thừa không hợp lệ");

                var currentFunds = await db.Clans.Where(x => x.Id == clanId)
                    .Select(x => x.Funds).SingleAsync();
                var newFunds = currentFunds - cost;
                if (newFunds < 0)
                    throw new InvalidException("Quỹ tông môn không đủ để thay đổi truyền thừa");

                if (cost > 0)
                {
                    var updated = await db.Clans
                        .Where(x => x.Id == clanId && x.Funds == currentFunds && x.Funds >= cost)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.Funds, newFunds));
                    if (updated != 1)
                        throw new InvalidException("Quỹ tông môn đã thay đổi, vui lòng thử lại");
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
                return new InheritanceTransactionResult(
                    request.EffectType == null ? null : persisted,
                    newFunds,
                    cost,
                    true);
            });

            clan.Funds = transactionResult.ClanFunds;
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
                if (transactionResult.Cost > 0)
                {
                    observer.SendAll(MessageTag.ClanFundsChanged,
                        new ClanFundsChanged { ClanId = clanId, Funds = transactionResult.ClanFunds });
                }
                observer.SendAll(MessageTag.ClanInheritanceChanged, change);
            }

            return new SetClanInheritanceResult
            {
                Success = true,
                Change = change,
                NewMoneyVip = actor.MoneyVip,
                ClanFunds = transactionResult.ClanFunds,
                Cost = transactionResult.Cost
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
            try { newExp = checked(clan.Exp + amount); }
            catch (OverflowException) { throw new InvalidException("Clan EXP đã đạt giới hạn"); }

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
                        x.Id == clanId && x.Exp == remainingExp && x.Level == newLevel && x.MaxMembers == newMaxMembers))
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
