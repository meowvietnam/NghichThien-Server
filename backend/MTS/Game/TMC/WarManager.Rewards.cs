using Microsoft.EntityFrameworkCore;
using MTS.Models;
using MTS.Network;
using Serilog;

namespace MTS.Game.TMC;

/// <summary>
/// Thưởng và mùa của Tông môn chiến.
/// <list type="bullet">
/// <item>Hằng ngày 00:00 (UTC+7): chụp số điểm (node thường, 100 tiên ngọc/ngày) và thành (Stronghold, 500/ngày) mỗi tông
/// đang giữ, gửi thư tiên ngọc: mỗi nhóm chức vụ nhận một phần %, chia đều cho thành viên đủ điều kiện trong nhóm:
/// Tông chủ 50%, Phó tông chủ 20%, Trưởng lão 10%, Thành viên 20%. Điều kiện: còn trong tông lúc chốt và có mặt
/// trong scene chiến trong ngày được chốt.</item>
/// <item>Mỗi mùa kéo dài 14 ngày, nối tiếp nhau. Hết mùa: node thường về trung lập, người và rối về thành chính,
/// HP hồi đầy; rối giữ level và độ bền.</item>
/// </list>
/// </summary>
public static partial class WarManager
{
    /// <summary>Các node Stronghold trên bản đồ (tính là "thành").</summary>
    private static readonly HashSet<int> StrongholdNodes = new();

    private static DateTime? lastSettledDay;
    private static DateTime? lastResetSeason;

    private static int CountHeld(int clanId, bool stronghold)
    {
        var count = 0;
        foreach (var node in Nodes.Values)
        {
            if (node.OwnerClanId != clanId || MainCities.ContainsKey(node.NodeId)) continue;
            if (StrongholdNodes.Contains(node.NodeId) == stronghold) count++;
        }

        return count;
    }

    /// <summary>Gọi định kỳ từ TongMonChienService: chốt thưởng ngày trước, rồi làm mới mùa nếu đã sang mùa mới.</summary>
    public static async Task RunScheduledSettlements()
    {
        if (!IsInitialized) return;
        await SettleDailyIfDue();
        await ResetSeasonIfDue();
    }

    private static async Task<DateTime?> LastPeriod(Data.AppDbContext db, WarSettlementKind kind) =>
        await db.WarSettlements.AsNoTracking()
            .Where(x => x.Kind == kind)
            .OrderByDescending(x => x.PeriodStart)
            .Select(x => (DateTime?)x.PeriodStart)
            .FirstOrDefaultAsync();

    private readonly record struct DailyClanScore(int ClanId, string Name, int Strongholds, int Nodes, List<int> Members);
    private readonly record struct DailyMembership(int CharId, int ClanId, ClanRole Role);

    // =====================================================================
    // Thưởng hằng ngày
    // =====================================================================

    private static async Task SettleDailyIfDue()
    {
        var now = DateTime.UtcNow;
        var dayToSettle = WarConfig.GetDayStart(now).AddDays(-1);
        if (lastSettledDay is { } settled && settled >= dayToSettle) return;

        await using var db = await Program.CreateDbAsync();
        var last = await LastPeriod(db, WarSettlementKind.Daily);
        if (last == null)
        {
            // Lần chạy đầu tiên sau khi triển khai: đặt mốc, chưa phát thưởng.
            db.WarSettlements.Add(new WarSettlement
                { Kind = WarSettlementKind.Daily, PeriodStart = dayToSettle, SettledAt = now });
            await db.SaveChangesAsync();
            lastSettledDay = dayToSettle;
            Log.Information("TongMonChien daily baseline set at {Day}", dayToSettle);
            return;
        }

        if (last.Value >= dayToSettle)
        {
            lastSettledDay = last.Value;
            return;
        }

        List<(int ClanId, string Name, int Strongholds, int Nodes)> held;
        lock (Sync)
        {
            held = Clans.Values.Select(c => (c.Id, c.Name, CountHeld(c.Id, true), CountHeld(c.Id, false)))
                .Where(x => x.Item3 + x.Item4 > 0)
                .ToList();
        }
        var clanIds = held.Select(x => x.ClanId).ToList();
        var memberships = (await db.ClanMembers.AsNoTracking()
                .Where(x => clanIds.Contains(x.ClanId))
                .Select(x => new { x.CharId, x.ClanId, x.Role })
                .ToListAsync())
            .Select(x => new DailyMembership(x.CharId, x.ClanId, x.Role))
            .ToList();
        var leaders = await db.Clans.AsNoTracking()
            .Where(x => clanIds.Contains(x.Id))
            .Select(x => new { x.Id, x.LeaderCharId })
            .ToDictionaryAsync(x => x.Id, x => x.LeaderCharId);
        var membershipOf = memberships.GroupBy(x => x.CharId).ToDictionary(g => g.Key, g => g.First());
        var scores = new List<DailyClanScore>();
        foreach (var h in held)
        {
            var members = memberships.Where(m => m.ClanId == h.ClanId).Select(m => m.CharId).Distinct().ToList();
            if (leaders.TryGetValue(h.ClanId, out var leaderId) && leaderId > 0 && !membershipOf.ContainsKey(leaderId))
            {
                // Tông chủ không có dòng ClanMembers vẫn được thưởng phần Tông chủ.
                members.Add(leaderId);
                membershipOf[leaderId] = new DailyMembership(leaderId, h.ClanId, ClanRole.Leader);
            }

            if (members.Count > 0)
                scores.Add(new DailyClanScore(h.ClanId, h.Name, h.Strongholds, h.Nodes, members));
        }

        var mails = new List<Mail>();
        long total = 0;
        var dayLocal = dayToSettle + Utils.Utils.Utc7;
        foreach (var score in scores)
        {
            var clanTotal = WarConfig.GetClanDailyMoneyVip(score.Strongholds, score.Nodes);
            if (clanTotal <= 0) continue;
            // Nhóm các thành viên đủ điều kiện theo chức vụ; phần % của nhóm chia đều cho người trong nhóm.
            var groups = score.Members
                .Where(charId => membershipOf.TryGetValue(charId, out var m) && m.ClanId == score.ClanId)
                .GroupBy(charId => leaders.TryGetValue(score.ClanId, out var leaderId) && leaderId == charId
                    ? ClanRole.Leader
                    : membershipOf[charId].Role == ClanRole.Leader ? ClanRole.Member : membershipOf[charId].Role);
            foreach (var group in groups)
            {
                var role = group.Key;
                var members = group.ToList();
                var amount = WarConfig.GetMemberDailyMoneyVip(clanTotal, role, members.Count);
                if (amount <= 0) continue;
                foreach (var charId in members)
                {
                    mails.Add(new Mail
                    {
                        CharId = charId,
                        Title = $"Thưởng Tông môn chiến ngày {dayLocal:dd/MM}",
                        Content = $"Tông môn {score.Name} giữ {score.Nodes} điểm và {score.Strongholds} thành, " +
                                  $"tổng {clanTotal} tiên ngọc/ngày. Nhóm {RoleName(role)} nhận " +
                                  $"{WarConfig.GetRewardPercent(role)}%, chia đều cho {members.Count} người: " +
                                  $"{amount} tiên ngọc.",
                        Rewards = [new MailRewardMoneyVip { MoneyVip = (int)Math.Min(amount, int.MaxValue) }],
                        CreatedAt = now
                    });
                    total += amount;
                }
            }
        }

        db.Mails.AddRange(mails);
        db.WarSettlements.Add(new WarSettlement
        {
            Kind = WarSettlementKind.Daily, PeriodStart = dayToSettle, SettledAt = now, ClanCount = scores.Count,
            MailCount = mails.Count, TotalMoneyVip = total
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            // Đã có tiến trình khác chốt ngày này (khoá chính) → bỏ qua.
            Log.Warning(e, "TongMonChien daily settlement {Day} already saved", dayToSettle);
            lastSettledDay = dayToSettle;
            return;
        }

        lastSettledDay = dayToSettle;
        Log.Information("TongMonChien daily settled {Day}: {Clans} clans, {Mails} mails, {Total} MoneyVip",
            dayToSettle, scores.Count, mails.Count, total);

        var onlineClients = TcpServer.Instance?.GetClientsOnline() ?? [];
        foreach (var mail in mails)
        {
            try
            {
                var client = onlineClients.FirstOrDefault(x => x.CharacterData?.Id == mail.CharId);
                client?.Send(MessageTag.NewMail, mail);
            }
            catch (Exception e)
            {
                Log.Warning(e, "TongMonChien daily mail {MailId} saved but notification failed", mail.Id);
            }
        }
    }

    private static string RoleName(ClanRole role) => role switch
    {
        ClanRole.Leader => "Tông chủ",
        ClanRole.ViceLeader => "Phó tông chủ",
        ClanRole.Elder => "Trưởng lão",
        _ => "Thành viên"
    };

    // =====================================================================
    // Làm mới mùa
    // =====================================================================

    private static async Task ResetSeasonIfDue()
    {
        var now = DateTime.UtcNow;
        var seasonStart = WarConfig.GetSeasonStart(now);
        if (lastResetSeason is { } reset && reset >= seasonStart) return;

        await using var db = await Program.CreateDbAsync();
        var last = await LastPeriod(db, WarSettlementKind.SeasonReset);
        if (last != null && last.Value >= seasonStart)
        {
            lastResetSeason = last.Value;
            return;
        }

        // Ghi mốc trước rồi mới làm mới, để restart giữa chừng không làm mới lần hai.
        db.WarSettlements.Add(new WarSettlement
            { Kind = WarSettlementKind.SeasonReset, PeriodStart = seasonStart, SettledAt = now });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            Log.Warning(e, "TongMonChien season reset {Season} already saved", seasonStart);
            lastResetSeason = seasonStart;
            return;
        }

        lastResetSeason = seasonStart;
        if (last == null)
        {
            // Lần chạy đầu tiên sau khi triển khai: chỉ đặt mốc mùa hiện tại, không làm mới.
            Log.Information("TongMonChien season baseline set at {Season}", seasonStart);
            return;
        }

        ResetTerritory(now);
        Log.Information("TongMonChien season reset at {Season}", seasonStart);
    }

    /// <summary>
    /// Làm mới đất: kết thúc mọi trận (hoà), node thường về trung lập, người và rối về thành chính với HP đầy.
    /// Rối giữ level và độ bền.
    /// </summary>
    private static void ResetTerritory(DateTime now)
    {
        lock (Sync)
        {
            foreach (var battle in Battles.Values.Where(x => !x.Ended).ToList())
            {
                EndBattle(battle, WarBattleOutcome.Draw, WarBattleEndReason.SeasonReset, null, null, now);
            }

            foreach (var node in Nodes.Values)
            {
                if (!MainCities.ContainsKey(node.NodeId)) SetOwner(node, null);
            }

            foreach (var unit in Units.Values.ToList())
            {
                Relocate(unit, MainCityOf(unit.ClanId));
                unit.Hp = unit.HpMax;
                unit.LastHpRecoveryAt = now;
                MarkUnit(unit);
            }

            Returning.Clear();
            PublishDelta();
        }

        foreach (var session in SnapshotSessions())
        {
            try
            {
                session.MemSend(MessageTag.WarNotice, new Network.Models.WarNotice
                {
                    Message = "Mùa Tông môn chiến đã kết thúc. Lãnh thổ được làm mới, mọi người trở về thành chính."
                });
            }
            catch (Exception e)
            {
                Log.Warning(e, "TongMonChien notify season reset failed");
            }
        }
    }

    private static List<IWarSession> SnapshotSessions()
    {
        lock (Sync) return Sessions.Values.ToList();
    }
}
