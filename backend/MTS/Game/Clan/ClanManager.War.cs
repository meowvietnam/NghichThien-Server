using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Models;

namespace MTS.Game.Clan;

/// <summary>Kết quả trả phí bằng điểm cống hiến + tiên ngọc (xem <see cref="ClanManager.PayWithContribOrMoneyVip"/>).</summary>
public sealed record ClanPaymentResult(
    bool RequiresConfirmation,
    long ContribPointsCost,
    long MoneyVipCost,
    long ContribPoints,
    int NewMoneyVip);

public static partial class ClanManager
{
    /// <summary>
    /// Đặt lại thành chính (Clan.NodeId) cho tông — thao tác quản trị. Node mong muốn đã bị tông khác chiếm thì
    /// chọn ngẫu nhiên node liền kề với lãnh thổ của tông (xem <see cref="TMC.WarManager.PickMainCityNode"/>).
    /// Sau khi đổi, mọi node tông đã chiếm (kể cả thành chính cũ) về trung lập, người và rối về thành chính mới.
    /// Trả về node được chọn.
    /// </summary>
    public static async Task<(int NodeId, string Note)> RelocateMainCity(int clanId, int targetNodeId)
    {
        var nodeId = TMC.WarManager.PickMainCityNode(clanId, targetNodeId, out var note);
        if (nodeId <= 0) throw new InvalidException(note);

        await using var db = await Program.CreateDbAsync();
        if (await db.Clans.AnyAsync(x => x.Id != clanId && x.NodeId == nodeId))
            throw new InvalidException($"Node {nodeId} đã là thành chính của tông khác.");
        var updated = await db.Clans.Where(x => x.Id == clanId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.NodeId, nodeId));
        if (updated == 0) throw new InvalidException("Tông môn không tồn tại");

        Models.Clan clan;
        if (References.TryGetValue(clanId, out var reference))
        {
            reference.Clan.NodeId = nodeId;
            clan = reference.Clan;
        }
        else
        {
            clan = await db.Clans.AsNoTracking().FirstAsync(x => x.Id == clanId);
        }
        PublishClanMapNodeChanged(clan);
        // Đổi vị trí thành chính: toàn bộ node đã chiếm của tông (kể cả thành chính cũ) về trung lập.
        TMC.WarManager.ResetClanTerritory(clanId);
        return (nodeId, note);
    }

    /// <summary>
    /// Trả chi phí tính bằng điểm cống hiến theo cách của mở giới hạn luyện hồn: trừ điểm cống hiến cá nhân trước,
    /// thiếu bao nhiêu điểm thì bù bằng tiên ngọc (2 điểm = 1 tiên ngọc, làm tròn lên).
    /// Nếu chi phí client đã xác nhận (<paramref name="expectedPointsCost"/>, <paramref name="expectedMoneyVipCost"/>)
    /// khác chi phí thực tế (kể cả lần hỏi giá với -1) thì không trừ gì và trả <c>RequiresConfirmation</c>.
    /// </summary>
    public static async Task<ClanPaymentResult> PayWithContribOrMoneyVip(Models.Clan clan, CharacterData actor,
        long totalPointsCost, long expectedPointsCost, long expectedMoneyVipCost, SpendFor spendFor)
    {
        if (totalPointsCost < 0) throw new InvalidException("Chi phí không hợp lệ");
        var actorMember = clan.Members.FirstOrDefault(x => x.CharId == actor.Id)
                          ?? throw new InvalidException("Chưa gia nhập tông môn");
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            var result = await ExecuteClanTransactionAsync(async db =>
            {
                var currentPoints = await db.ClanMembers
                    .Where(x => x.ClanId == clanId && x.CharId == actor.Id)
                    .Select(x => x.ContribPoints).SingleAsync();
                var pointsCost = Math.Min(Math.Max(0, currentPoints), totalPointsCost);
                var shortfall = totalPointsCost - pointsCost;
                var moneyVipCost = shortfall / 2 + shortfall % 2;
                if (expectedPointsCost != pointsCost || expectedMoneyVipCost != moneyVipCost)
                    return new ClanPaymentResult(true, pointsCost, moneyVipCost, currentPoints, actor.MoneyVip);
                if (moneyVipCost > actor.MoneyVip)
                    throw new InvalidException("Không đủ Tiên Ngọc để bù điểm cống hiến còn thiếu");
                var newMoneyVip = actor.MoneyVip - (int)moneyVipCost;
                var newPoints = currentPoints - pointsCost;
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
                        CharId = actor.Id, SpendFor = spendFor, MoneyVip = moneyVipCost, Time = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }

                return new ClanPaymentResult(false, pointsCost, moneyVipCost, newPoints, newMoneyVip);
            });

            actorMember.ContribPoints = result.ContribPoints;
            if (result.RequiresConfirmation) return result;
            actor.SetMoneyVip(result.NewMoneyVip);
            if (result.ContribPointsCost > 0) NotifyClanMemberPointsChanged(clanId, actor.Id, result.ContribPoints);
            return result;
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }

    /// <summary>Hoàn lại khoản đã trả bằng <see cref="PayWithContribOrMoneyVip"/> khi thao tác sau đó không thực hiện được.</summary>
    public static async Task Refund(Models.Clan clan, CharacterData actor, ClanPaymentResult payment)
    {
        if (payment.RequiresConfirmation || payment.ContribPointsCost + payment.MoneyVipCost <= 0) return;
        var clanId = clan.Id;
        var gate = AcquireGate(clanId);
        await gate.Semaphore.WaitAsync();
        try
        {
            await using var db = await Program.CreateDbAsync();
            if (payment.ContribPointsCost > 0)
                await db.ClanMembers.Where(x => x.ClanId == clanId && x.CharId == actor.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(c => c.ContribPoints, c => c.ContribPoints + payment.ContribPointsCost));
            if (payment.MoneyVipCost > 0)
                await db.Characters.Where(x => x.Id == actor.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(c => c.MoneyVip, c => c.MoneyVip + (int)payment.MoneyVipCost));
            var member = clan.Members.FirstOrDefault(x => x.CharId == actor.Id);
            if (member != null)
            {
                member.ContribPoints += payment.ContribPointsCost;
                if (payment.ContribPointsCost > 0) NotifyClanMemberPointsChanged(clanId, actor.Id, member.ContribPoints);
            }

            actor.SetMoneyVip(actor.MoneyVip + (int)payment.MoneyVipCost);
        }
        finally
        {
            ReleaseGate(clanId, gate);
        }
    }
}
