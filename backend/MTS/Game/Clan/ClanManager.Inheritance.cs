using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Game.BaseSkill;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;

namespace MTS.Game.Clan;

public static partial class ClanManager
{
    private sealed record InheritanceTransactionResult(
        ClanInheritanceEffect? Effect, int MoneyVip, bool Changed);

    public static int GetInheritanceSlots(int clanLevel)
    {
        if (Config.BaseInheritanceSlots < 0 || Config.InheritanceSlotsPerLevel < 0 ||
            Config.MaxInheritanceSlotsPerColumn < 0)
            throw new InvalidException("Cấu hình số ô truyền thừa không hợp lệ");

        var slots = (long)Config.BaseInheritanceSlots +
                    (long)Math.Max(0, clanLevel - 1) * Config.InheritanceSlotsPerLevel;
        return (int)Math.Clamp(slots, 0, Config.MaxInheritanceSlotsPerColumn);
    }

    public static int GetInheritanceValue(int clanLevel)
    {
        if (Config.InheritanceBaseValue < 0 || Config.InheritanceValuePerClanLevel < 0)
            throw new InvalidException("Cấu hình giá trị truyền thừa không hợp lệ");

        try
        {
            return checked(Config.InheritanceBaseValue +
                           Math.Max(0, clanLevel - 1) * Config.InheritanceValuePerClanLevel);
        }
        catch (OverflowException)
        {
            throw new InvalidException("Giá trị truyền thừa vượt giới hạn");
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
        if (request.BuffType != EBuffAndDeBuff.None &&
            !Config.InheritanceBuffWhitelist.Contains(request.BuffType))
            throw new InvalidException("Hiệu ứng không nằm trong danh sách truyền thừa cho phép");

        var actorMember = clan.Members.FirstOrDefault(x => x.CharId == actor.Id)
            ?? throw new InvalidException("Chưa gia nhập tông môn");
        if (!actorMember.HasPermission(ClanPermission.ManageInheritance))
            throw new InvalidException("Chỉ Tông Chủ được quản lý truyền thừa");

        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var current = clan.InheritanceEffects.FirstOrDefault(x =>
                x.Trigger == request.Trigger && x.SlotIndex == request.SlotIndex);
            var oldMoneyVip = actor.MoneyVip;
            var desiredValue = GetInheritanceValue(clan.Level);
            var ramChanged = request.BuffType == EBuffAndDeBuff.None
                ? current != null
                : current == null || current.BuffType != request.BuffType || current.Value != desiredValue;
            var changingFilledSlot = current != null && current.BuffType != request.BuffType;
            var expectedCost = changingFilledSlot ? Config.ChangeInheritanceMoneyVipCost : 0;
            if (expectedCost < 0)
                throw new InvalidException("Cấu hình phí thay đổi truyền thừa không hợp lệ");
            if (actor.MoneyVip < expectedCost)
                throw new InvalidException("Không đủ tiên ngọc để thay đổi truyền thừa");

            var value = desiredValue;
            var transactionResult = await ExecuteClanTransactionAsync(async db =>
            {
                var persisted = await db.Set<ClanInheritanceEffect>().FirstOrDefaultAsync(x =>
                    x.ClanId == clanId && x.Trigger == request.Trigger && x.SlotIndex == request.SlotIndex);

                var alreadyDesired = request.BuffType == EBuffAndDeBuff.None
                    ? persisted == null
                    : persisted?.BuffType == request.BuffType && persisted.Value == value;
                if (alreadyDesired)
                {
                    var currentMoney = await db.Characters.Where(x => x.Id == actor.Id)
                        .Select(x => x.MoneyVip).SingleAsync();
                    return new InheritanceTransactionResult(persisted, currentMoney, false);
                }

                var cost = persisted != null ? Config.ChangeInheritanceMoneyVipCost : 0;
                if (cost < 0)
                    throw new InvalidException("Cấu hình phí thay đổi truyền thừa không hợp lệ");

                var currentMoneyVip = await db.Characters.Where(x => x.Id == actor.Id)
                    .Select(x => x.MoneyVip).SingleAsync();
                var newMoneyVip = currentMoneyVip - cost;
                if (newMoneyVip < 0)
                    throw new InvalidException("Không đủ tiên ngọc để thay đổi truyền thừa");

                if (cost > 0)
                {
                    var updated = await db.Characters
                        .Where(x => x.Id == actor.Id && x.MoneyVip == currentMoneyVip)
                        .ExecuteUpdateAsync(x => x.SetProperty(c => c.MoneyVip, newMoneyVip));
                    if (updated != 1)
                        throw new InvalidException("Tiên ngọc đã thay đổi, vui lòng thử lại");
                    await db.SpendingMoneyVips.AddAsync(new SpendingMoneyVip
                    {
                        CharId = actor.Id,
                        SpendFor = SpendFor.ChangeClanInheritance,
                        MoneyVip = cost,
                        Time = DateTime.UtcNow
                    });
                }

                if (request.BuffType == EBuffAndDeBuff.None)
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
                        BuffType = request.BuffType,
                        Value = value
                    };
                    await db.AddAsync(persisted);
                }
                else
                {
                    persisted.BuffType = request.BuffType;
                    persisted.Value = value;
                }

                await db.SaveChangesAsync();
                return new InheritanceTransactionResult(
                    request.BuffType == EBuffAndDeBuff.None ? null : persisted,
                    newMoneyVip,
                    true);
            });

            actor.SetMoneyVip(transactionResult.MoneyVip);
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
                observer.SendAll(MessageTag.ClanInheritanceChanged, change);

            return new SetClanInheritanceResult
            {
                Success = true,
                Change = change,
                NewMoneyVip = transactionResult.MoneyVip,
                Cost = Math.Max(0, oldMoneyVip - transactionResult.MoneyVip)
            };
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
            try { newExp = checked(clan.Exp + amount); }
            catch (OverflowException) { throw new InvalidException("Clan EXP đã đạt giới hạn"); }

            var newLevel = Math.Max(clan.Level, ResolveClanLevel(newExp));
            var newValue = GetInheritanceValue(newLevel);
            await ExecuteClanTransactionAsync(async db =>
            {
                var updated = await db.Clans
                    .Where(x => x.Id == clanId && x.Exp == clan.Exp && x.Level == clan.Level)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Exp, newExp)
                        .SetProperty(c => c.Level, newLevel));
                if (updated != 1 && !await db.Clans.AnyAsync(x =>
                        x.Id == clanId && x.Exp == newExp && x.Level == newLevel))
                    throw new InvalidException("Dữ liệu cấp tông môn đã thay đổi, vui lòng thử lại");

                if (newLevel != clan.Level)
                {
                    await db.Set<ClanInheritanceEffect>().Where(x => x.ClanId == clanId)
                        .ExecuteUpdateAsync(x => x.SetProperty(e => e.Value, newValue));
                }
                return true;
            });

            var levelChanged = clan.Level != newLevel;
            clan.Exp = newExp;
            clan.Level = newLevel;
            if (levelChanged)
            {
                foreach (var effect in clan.InheritanceEffects) effect.Value = newValue;
                clan.RefreshInheritanceViews();
            }

            if (Observers.TryGetValue(clanId, out var observer))
            {
                observer.SendAll(MessageTag.ClanExpChanged,
                    new ClanExpChanged { ClanId = clanId, Exp = newExp });
                if (levelChanged)
                {
                    observer.SendAll(MessageTag.ClanLevelChanged, new ClanLevelChanged
                    {
                        ClanId = clanId,
                        Level = newLevel,
                        Exp = newExp,
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

    private static int ResolveClanLevel(long totalExp)
    {
        var milestones = Config.ClanLevelRequiredTotalExp;
        if (milestones.Length == 0) return 1;
        if (milestones[0] != 0)
            throw new InvalidException("Mốc EXP cấp 1 của tông môn phải bằng 0");

        var level = 1;
        for (var index = 0; index < milestones.Length; index++)
        {
            if (milestones[index] < 0 || index > 0 && milestones[index] <= milestones[index - 1])
                throw new InvalidException("Các mốc EXP tông môn phải tăng dần và không âm");
            if (totalExp < milestones[index]) break;
            level = index + 1;
        }
        return level;
    }
}
