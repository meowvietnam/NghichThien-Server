using Microsoft.EntityFrameworkCore;
using MTS.Models;
using MTS.Network;
using Serilog;

namespace MTS.Game.TMC;

/// <summary>
/// Thưởng tuần Tông môn chiến. Lúc 00:00 thứ Hai (UTC+7) server chụp số Thành Ấn (thành Stronghold) và
/// Địa Mạch (node thường) mỗi tông đang giữ rồi gửi thư tiên ngọc cho từng thành viên đủ điều kiện:
/// còn trong tông lúc chốt và đã vào scene chiến ít nhất một lần trong tuần. Mỗi thành viên nhận đủ phần của tông.
/// Điểm tuần tự về 0 vì được tính lại từ lãnh thổ lúc chốt; lãnh thổ giữ nguyên.
/// </summary>
public static partial class WarManager
{
    /// <summary>Các node Stronghold trên bản đồ (tính Thành Ấn).</summary>
    private static readonly HashSet<int> StrongholdNodes = new();

    private static DateTime? lastSettledWeek;

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

    private readonly record struct WeeklyClanScore(int ClanId, string Name, int Strongholds, int Nodes, List<int> Members);

    /// <summary>
    /// Chốt thưởng tuần nếu đã qua mốc. Gọi định kỳ từ TongMonChienService; an toàn khi gọi lặp lại
    /// (mỗi tuần có một bản ghi <see cref="WarWeeklySettlement"/> lưu cùng thư thưởng).
    /// </summary>
    public static async Task SettleWeeklyIfDue()
    {
        if (!IsInitialized) return;
        var now = DateTime.UtcNow;
        // Tuần vừa kết thúc bắt đầu 7 ngày trước mốc tuần hiện tại.
        var weekToSettle = WarConfig.GetWeekStart(now).AddDays(-7);
        if (lastSettledWeek is { } settled && settled >= weekToSettle) return;

        await using var db = await Program.CreateDbAsync();
        var last = await db.WarWeeklySettlements.AsNoTracking()
            .OrderByDescending(x => x.WeekStart).Select(x => (DateTime?)x.WeekStart).FirstOrDefaultAsync();
        if (last == null)
        {
            // Lần chạy đầu tiên sau khi triển khai: đặt mốc, không phát thưởng cho tuần chưa có luật.
            db.WarWeeklySettlements.Add(new WarWeeklySettlement { WeekStart = weekToSettle, SettledAt = now });
            await db.SaveChangesAsync();
            lastSettledWeek = weekToSettle;
            Log.Information("TongMonChien weekly baseline set at {Week}", weekToSettle);
            return;
        }

        if (last.Value >= weekToSettle)
        {
            lastSettledWeek = last.Value;
            return;
        }

        // Chụp lãnh thổ và người tham chiến dưới Sync.
        List<WeeklyClanScore> scores;
        lock (Sync)
        {
            scores = Clans.Values.Select(c => new WeeklyClanScore(c.Id, c.Name, CountHeld(c.Id, true),
                    CountHeld(c.Id, false),
                    Units.Values.Where(u => !u.IsPuppet && u.ClanId == c.Id &&
                                            (Sessions.ContainsKey(u.Id) ||
                                             u.Player!.LastEnterAt is { } at && at >= weekToSettle))
                        .Select(u => u.Id).ToList()))
                .Where(x => x.Strongholds + x.Nodes > 0 && x.Members.Count > 0)
                .ToList();
        }

        // Chỉ thành viên còn trong tông lúc chốt.
        var candidateIds = scores.SelectMany(x => x.Members).Distinct().ToList();
        var membership = await db.ClanMembers.AsNoTracking()
            .Where(x => candidateIds.Contains(x.CharId))
            .Select(x => new { x.CharId, x.ClanId })
            .ToListAsync();
        var clanOf = membership.ToDictionary(x => x.CharId, x => x.ClanId);

        var mails = new List<Mail>();
        long total = 0;
        foreach (var score in scores)
        {
            var amount = WarConfig.GetWeeklyMoneyVip(score.Strongholds, score.Nodes);
            if (amount <= 0) continue;
            foreach (var charId in score.Members)
            {
                if (!clanOf.TryGetValue(charId, out var clanId) || clanId != score.ClanId) continue;
                mails.Add(new Mail
                {
                    CharId = charId,
                    Title = "Thưởng Tông môn chiến tuần",
                    Content = $"Tông môn {score.Name} giữ {score.Strongholds} {WarConfig.StrongholdPointName} " +
                              $"và {score.Nodes} {WarConfig.NodePointName} khi chốt tuần. " +
                              $"Đạo hữu nhận {amount} tiên ngọc.",
                    Rewards = [new MailRewardMoneyVip { MoneyVip = amount }],
                    CreatedAt = now
                });
                total += amount;
            }
        }

        db.Mails.AddRange(mails);
        db.WarWeeklySettlements.Add(new WarWeeklySettlement
        {
            WeekStart = weekToSettle, SettledAt = now, ClanCount = scores.Count, MailCount = mails.Count,
            TotalMoneyVip = total
        });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e)
        {
            // Đã có tiến trình khác chốt tuần này (khoá chính WeekStart) → bỏ qua.
            Log.Warning(e, "TongMonChien weekly settlement {Week} already saved", weekToSettle);
            lastSettledWeek = weekToSettle;
            return;
        }

        lastSettledWeek = weekToSettle;
        Log.Information("TongMonChien weekly settled {Week}: {Clans} clans, {Mails} mails, {Total} MoneyVip",
            weekToSettle, scores.Count, mails.Count, total);

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
                Log.Warning(e, "TongMonChien weekly mail {MailId} saved but notification failed", mail.Id);
            }
        }
    }
}
