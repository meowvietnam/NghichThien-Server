using Microsoft.EntityFrameworkCore;
using MTS.Exceptions;
using MTS.Game.TMC.Achievements;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;

namespace MTS.Game.TMC;

/// <summary>
/// Thành tựu Tông môn chiến (theo nhân vật, tính theo mùa 2 tuần — sang mùa mới làm mới về 0): chiếm điểm, chiếm thành,
/// ghép trận, thắng trận.
/// Mốc và phần thưởng cấu hình trong <c>Data/war-achievements.json</c>. Tiến độ nằm trong bộ nhớ dưới
/// <see cref="Sync"/>, ghi DB cùng nhịp <see cref="Flush"/>. Giao diện client dùng lại bảng thành tựu Động phủ.
/// </summary>
public static partial class WarManager
{
    /// <summary>Stat gửi cho client = 100 + WarStat, để không trùng DongPhuStat (client xử lý riêng stat 6).</summary>
    private const byte WarStatWireOffset = 100;

    private static readonly Dictionary<(int CharId, WarStat Stat), WarAchievementProgress> achievementProgress = new();
    private static readonly HashSet<(int, WarStat)> persistedAchievements = new();
    private static readonly HashSet<(int, WarStat)> dirtyAchievements = new();

    private static void LoadAchievements(List<WarAchievementProgress> rows)
    {
        foreach (var row in rows)
        {
            achievementProgress[(row.CharId, row.Stat)] = row;
            persistedAchievements.Add((row.CharId, row.Stat));
        }
    }

    private static void AddAchievementStat(WarUnit unit, WarStat stat)
    {
        if (!unit.IsPuppet) AddAchievementStat(unit.Id, stat);
    }

    /// <summary>Cộng chỉ số; vừa đạt mốc mới thì báo chấm đỏ cho người chơi đang ở scene chiến.</summary>
    private static void AddAchievementStat(int charId, WarStat stat)
    {
        var key = (charId, stat);
        if (!achievementProgress.TryGetValue(key, out var p))
        {
            p = new WarAchievementProgress { CharId = charId, Stat = stat };
            achievementProgress[key] = p;
        }

        var before = p.Value;
        p.Value++;
        dirtyAchievements.Add(key);
        var crossed = WarAchievementConfig.Current.Achievements
            .Where(a => a.Stat == stat)
            .Any(a => a.Tiers.Any(t => before < t.Target && p.Value >= t.Target));
        if (!crossed || !Sessions.TryGetValue(charId, out var session)) return;
        try
        {
            session.MemSend(MessageTag.WarAchievementNotify,
                new DongPhuAchievementNotify { ClaimableCount = CountClaimableUnlocked(charId) });
        }
        catch (Exception e)
        {
            Log.Warning(e, "TongMonChien achievement notify failed; CharId={CharId}", charId);
        }
    }

    /// <summary>Node vừa về tay một tông: cộng "chiếm điểm/thành" cho người chơi của tông đó đang có mặt tại node.</summary>
    private static void CreditCapture(int nodeId, List<WarUnit> present)
    {
        if (MainCities.ContainsKey(nodeId)) return;
        var owner = OwnerOf(nodeId);
        if (owner == null) return;
        var stat = StrongholdNodes.Contains(nodeId) ? WarStat.CaptureStronghold : WarStat.CaptureNode;
        foreach (var unit in present.Where(x => !x.IsPuppet && x.ClanId == owner))
            AddAchievementStat(unit.Id, stat);
    }

    private static long AchievementValue(int charId, WarStat stat) =>
        achievementProgress.TryGetValue((charId, stat), out var p) ? p.Value : 0;

    private static string ClaimKey(string achievementId, int tierId) => $"{achievementId}:{tierId}";

    private static bool IsAchievementClaimed(int charId, WarAchievementDefinition a, int tierId) =>
        achievementProgress.TryGetValue((charId, a.Stat), out var p) && !string.IsNullOrEmpty(p.ClaimedTiers) &&
        p.ClaimedTiers.Split(';').Contains(ClaimKey(a.Id, tierId));

    private static int CountClaimableUnlocked(int charId) =>
        WarAchievementConfig.Current.Achievements.Sum(a =>
        {
            var value = AchievementValue(charId, a.Stat);
            return a.Tiers.Count(t => value >= t.Target && !IsAchievementClaimed(charId, a, t.Id));
        });

    /// <summary>Số mốc đã đạt nhưng chưa nhận (chấm đỏ trên nút Thành tựu).</summary>
    public static int CountClaimableAchievements(int charId)
    {
        lock (Sync) return CountClaimableUnlocked(charId);
    }

    /// <summary>Danh sách thành tựu kèm tiến độ và trạng thái từng mốc của nhân vật.</summary>
    public static List<DongPhuAchievementView> GetAchievements(int charId)
    {
        lock (Sync)
        {
            return WarAchievementConfig.Current.Achievements.Select(a => new DongPhuAchievementView
            {
                Id = a.Id,
                Stat = (byte)(WarStatWireOffset + (byte)a.Stat),
                Name = a.Name,
                Description = a.Description,
                Value = AchievementValue(charId, a.Stat),
                Tiers = a.Tiers.Select(t => new DongPhuAchievementTierView
                {
                    Id = t.Id,
                    Target = t.Target,
                    IsClaimed = IsAchievementClaimed(charId, a, t.Id),
                    Rewards = t.CreateRewards()
                }).ToList()
            }).ToList();
        }
    }

    /// <summary>
    /// Nhận thưởng một mốc: kiểm tra đã đạt và chưa nhận, đánh dấu đã nhận rồi trả phần thưởng để phát.
    /// Người gọi phải gọi <see cref="FlushAchievements"/> trước khi phát thưởng.
    /// </summary>
    public static List<IEventReward> ClaimAchievement(int charId, string achievementId, int tierId)
    {
        lock (Sync)
        {
            var a = WarAchievementConfig.Current.Find(achievementId)
                    ?? throw new InvalidException("Thành tựu không tồn tại");
            var tier = a.Tiers.FirstOrDefault(x => x.Id == tierId)
                       ?? throw new InvalidException("Mốc thành tựu không tồn tại");
            if (AchievementValue(charId, a.Stat) < tier.Target) throw new InvalidException("Chưa đạt mốc thành tựu");
            if (IsAchievementClaimed(charId, a, tierId)) throw new InvalidException("Đã nhận thưởng mốc này");
            var key = (charId, a.Stat);
            var p = achievementProgress[key];
            var claim = ClaimKey(a.Id, tierId);
            p.ClaimedTiers = string.IsNullOrEmpty(p.ClaimedTiers) ? claim : $"{p.ClaimedTiers};{claim}";
            dirtyAchievements.Add(key);
            return tier.CreateRewards();
        }
    }

    /// <summary>
    /// Làm mới thành tựu khi sang mùa mới (2 tuần): xoá toàn bộ tiến độ và mốc đã nhận của mọi nhân vật.
    /// Mốc đã đạt mà chưa nhận ở mùa cũ sẽ mất.
    /// </summary>
    private static async Task ResetAchievements()
    {
        List<IWarSession> sessions;
        lock (Sync)
        {
            achievementProgress.Clear();
            persistedAchievements.Clear();
            dirtyAchievements.Clear();
            sessions = Sessions.Values.ToList();
        }

        try
        {
            await using var db = await Program.CreateDbAsync();
            var deleted = await db.WarAchievementProgresses.ExecuteDeleteAsync();
            Log.Information("TongMonChien achievements reset for new season: {Rows} rows removed", deleted);
        }
        catch (Exception e)
        {
            Log.Error(e, "TongMonChien reset achievements failed (memory already cleared)");
        }

        foreach (var session in sessions)
        {
            try
            {
                session.MemSend(MessageTag.WarAchievementNotify, new DongPhuAchievementNotify { ClaimableCount = 0 });
            }
            catch (Exception e)
            {
                Log.Warning(e, "TongMonChien achievement notify failed; CharId={CharId}", session.CharId);
            }
        }
    }

    /// <summary>Ghi tiến độ thành tựu đã đổi xuống DB. Lỗi thì đánh dấu lại để ghi ở nhịp sau.</summary>
    public static async Task FlushAchievements()
    {
        List<WarAchievementProgress> rows;
        HashSet<(int, WarStat)> known;
        lock (Sync)
        {
            if (!initialized || dirtyAchievements.Count == 0) return;
            rows = dirtyAchievements.Where(achievementProgress.ContainsKey).Select(k => achievementProgress[k])
                .Select(p => new WarAchievementProgress
                {
                    CharId = p.CharId, Stat = p.Stat, Value = p.Value, ClaimedTiers = p.ClaimedTiers
                }).ToList();
            dirtyAchievements.Clear();
            known = new HashSet<(int, WarStat)>(persistedAchievements);
        }

        try
        {
            await using var db = await Program.CreateDbAsync();
            foreach (var row in rows)
            {
                if (known.Contains((row.CharId, row.Stat))) db.WarAchievementProgresses.Update(row);
                else db.WarAchievementProgresses.Add(row);
            }

            await db.SaveChangesAsync();
            lock (Sync)
            {
                foreach (var row in rows) persistedAchievements.Add((row.CharId, row.Stat));
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "TongMonChien flush achievements failed; retry next tick");
            lock (Sync)
            {
                foreach (var row in rows)
                {
                    // Dòng đã bị xoá khi làm mới mùa thì bỏ; còn lại ghi lại ở nhịp sau.
                    if (!achievementProgress.ContainsKey((row.CharId, row.Stat))) continue;
                    dirtyAchievements.Add((row.CharId, row.Stat));
                    // Trùng khoá (dòng đã có trong DB mà chưa được đánh dấu): lần sau ghi dạng cập nhật.
                    if (e is Microsoft.EntityFrameworkCore.DbUpdateException && !known.Contains((row.CharId, row.Stat)))
                        persistedAchievements.Add((row.CharId, row.Stat));
                }
            }
        }
    }
}
