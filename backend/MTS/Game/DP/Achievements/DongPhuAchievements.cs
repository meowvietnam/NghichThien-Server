using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Exceptions;
using MTS.Models;
using MTS.Network.Models;
using static MTS.Utils.Utils;

namespace MTS.Game.DP.Achievements;

/// <summary>
/// Tiến độ thành tựu Động phủ của một nhân vật (gắn với <see cref="DongPhu"/> của nhân vật đó, nạp/lưu cùng động phủ).
/// Mọi chỉ số đếm trọn đời; riêng <see cref="DongPhuStat.Level"/> đọc trực tiếp từ cấp động phủ.
/// </summary>
public class DongPhuAchievements
{
    private readonly object sync = new();
    private readonly int charId;
    private readonly Func<DongPhuLevel> levelProvider;
    private readonly Dictionary<DongPhuStat, DongPhuAchievementProgress> progresses = new();
    private readonly HashSet<DongPhuStat> persisted = new();
    private readonly HashSet<DongPhuStat> dirty = new();
    /// <summary>Các dòng đang được ghi (đã đưa vào DbContext nhưng chưa xác nhận lưu xong).</summary>
    private readonly HashSet<DongPhuStat> saving = new();
    /// <summary>Động phủ đã thăm hôm nay (mỗi động phủ chỉ tính 1 lần/ngày). Chỉ giữ trong bộ nhớ.</summary>
    private readonly HashSet<int> visitedToday = new();
    private DateTime visitedDay;

    public DongPhuAchievements(int charId, Func<DongPhuLevel> levelProvider)
    {
        this.charId = charId;
        this.levelProvider = levelProvider;
    }

    /// <summary>
    /// Nạp tiến độ từ DB. Lần đầu (chưa có dòng nào) lấy lại số liệu sẵn có: lượt like nhận được và lượt like đã cho;
    /// các chỉ số khác bắt đầu từ 0.
    /// </summary>
    public async Task Load(AppDbContext db, int likeReceived)
    {
        var rows = await db.DongPhuAchievementProgresses.AsNoTracking()
            .Where(x => x.CharId == charId).ToListAsync();
        var likeGiven = rows.All(x => x.Stat != DongPhuStat.LikeGiven)
            ? await db.DongPhuLikes.CountAsync(x => x.OtherCharId == charId && x.OwnerCharId != charId)
            : 0;
        lock (sync)
        {
            foreach (var row in rows)
            {
                progresses[row.Stat] = row;
                persisted.Add(row.Stat);
            }

            Backfill(DongPhuStat.LikeReceived, likeReceived);
            Backfill(DongPhuStat.LikeGiven, likeGiven);
        }
    }

    private void Backfill(DongPhuStat stat, long value)
    {
        if (progresses.ContainsKey(stat) || value <= 0) return;
        progresses[stat] = new DongPhuAchievementProgress { CharId = charId, Stat = stat, Value = value };
        dirty.Add(stat);
    }

    public long GetValue(DongPhuStat stat)
    {
        if (stat == DongPhuStat.Level) return (long)levelProvider();
        lock (sync) return progresses.TryGetValue(stat, out var p) ? p.Value : 0;
    }

    /// <summary>Cộng chỉ số. Trả true nếu vừa có thêm mốc đủ điều kiện nhận (để báo chấm đỏ).</summary>
    public bool Add(DongPhuStat stat, long amount = 1)
    {
        if (amount <= 0 || stat == DongPhuStat.Level) return false;
        lock (sync)
        {
            if (!progresses.TryGetValue(stat, out var p))
            {
                p = new DongPhuAchievementProgress { CharId = charId, Stat = stat };
                progresses[stat] = p;
            }

            var before = p.Value;
            p.Value += amount;
            dirty.Add(stat);
            return CrossedTier(stat, before, p.Value);
        }
    }

    /// <summary>Thăm động phủ <paramref name="targetCharId"/>: mỗi động phủ chỉ tính 1 lần/ngày, không tính động phủ của mình.</summary>
    public bool AddVisit(int targetCharId)
    {
        if (targetCharId == charId) return false;
        lock (sync)
        {
            if (!IsToday(visitedDay))
            {
                visitedToday.Clear();
                visitedDay = DateTime.UtcNow;
            }

            if (!visitedToday.Add(targetCharId)) return false;
        }

        return Add(DongPhuStat.Visit);
    }

    private static bool CrossedTier(DongPhuStat stat, long before, long after) =>
        DongPhuAchievementConfig.Current.Achievements
            .Where(a => a.Stat == stat)
            .Any(a => a.Tiers.Any(t => before < t.Target && after >= t.Target));

    private static string ClaimKey(string achievementId, int tierId) => $"{achievementId}:{tierId}";

    private bool IsClaimed(DongPhuAchievementDefinition a, int tierId)
    {
        if (!progresses.TryGetValue(a.Stat, out var p) || string.IsNullOrEmpty(p.ClaimedTiers)) return false;
        var key = ClaimKey(a.Id, tierId);
        return p.ClaimedTiers.Split(';').Contains(key);
    }

    /// <summary>Số mốc đã đạt nhưng chưa nhận (chấm đỏ).</summary>
    public int CountClaimable()
    {
        lock (sync)
        {
            return DongPhuAchievementConfig.Current.Achievements.Sum(a =>
            {
                var value = GetValueUnlocked(a.Stat);
                return a.Tiers.Count(t => value >= t.Target && !IsClaimed(a, t.Id));
            });
        }
    }

    private long GetValueUnlocked(DongPhuStat stat) =>
        stat == DongPhuStat.Level ? (long)levelProvider() : progresses.TryGetValue(stat, out var p) ? p.Value : 0;

    /// <summary>Dựng danh sách thành tựu kèm tiến độ và trạng thái từng mốc để gửi client.</summary>
    public List<DongPhuAchievementView> BuildViews()
    {
        lock (sync)
        {
            return DongPhuAchievementConfig.Current.Achievements.Select(a =>
            {
                var value = GetValueUnlocked(a.Stat);
                return new DongPhuAchievementView
                {
                    Id = a.Id,
                    Stat = (byte)a.Stat,
                    Name = a.Name,
                    Description = a.Description,
                    Value = value,
                    Tiers = a.Tiers.Select(t => new DongPhuAchievementTierView
                    {
                        Id = t.Id,
                        Target = t.Target,
                        IsClaimed = IsClaimed(a, t.Id),
                        Rewards = t.CreateRewards()
                    }).ToList()
                };
            }).ToList();
        }
    }

    /// <summary>Nhận thưởng một mốc: kiểm tra đã đạt và chưa nhận, đánh dấu đã nhận rồi trả phần thưởng để phát.</summary>
    public List<IEventReward> Claim(string achievementId, int tierId)
    {
        lock (sync)
        {
            var a = DongPhuAchievementConfig.Current.Find(achievementId)
                    ?? throw new InvalidException("Thành tựu không tồn tại");
            var tier = a.Tiers.FirstOrDefault(x => x.Id == tierId)
                       ?? throw new InvalidException("Mốc thành tựu không tồn tại");
            if (GetValueUnlocked(a.Stat) < tier.Target) throw new InvalidException("Chưa đạt mốc thành tựu");
            if (IsClaimed(a, tierId)) throw new InvalidException("Đã nhận thưởng mốc này");
            if (!progresses.TryGetValue(a.Stat, out var p))
            {
                // Chỉ số Level không có dòng tiến độ: tạo dòng để lưu các mốc đã nhận.
                p = new DongPhuAchievementProgress { CharId = charId, Stat = a.Stat };
                progresses[a.Stat] = p;
            }

            var key = ClaimKey(a.Id, tierId);
            p.ClaimedTiers = string.IsNullOrEmpty(p.ClaimedTiers) ? key : $"{p.ClaimedTiers};{key}";
            dirty.Add(a.Stat);
            return tier.CreateRewards();
        }
    }

    /// <summary>Ghi các dòng đã đổi vào <paramref name="db"/> (SaveChanges do DongPhu.SaveChanges gọi chung).</summary>
    public void SaveChanges(AppDbContext db)
    {
        lock (sync)
        {
            // Lần ghi trước lỗi (chưa AcceptChanges): ghi lại.
            dirty.UnionWith(saving);
            saving.Clear();
            foreach (var stat in dirty)
            {
                var p = progresses[stat];
                var row = new DongPhuAchievementProgress
                {
                    CharId = p.CharId, Stat = p.Stat, Value = p.Value, ClaimedTiers = p.ClaimedTiers
                };
                if (persisted.Contains(stat)) db.DongPhuAchievementProgresses.Update(row);
                else db.DongPhuAchievementProgresses.Add(row);
            }

            // Thay đổi xảy ra trong lúc đang ghi sẽ vào lại dirty và được ghi ở lần sau.
            saving.UnionWith(dirty);
            dirty.Clear();
        }
    }

    /// <summary>Gọi sau khi SaveChanges thành công.</summary>
    public void AcceptChanges()
    {
        lock (sync)
        {
            persisted.UnionWith(saving);
            saving.Clear();
        }
    }
}
