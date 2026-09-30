using MTS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MTS.Game.DP.Achievements;

/// <summary>Một mốc của thành tựu: đạt <see cref="Target"/> thì được nhận <see cref="Rewards"/>.</summary>
public class DongPhuAchievementTierConfig
{
    /// <summary>Id mốc, duy nhất trong thành tựu. Đừng đổi Id của mốc đã phát hành (dùng để nhớ đã nhận).</summary>
    public int Id { get; set; }
    public long Target { get; set; }
    /// <summary>
    /// Phần thưởng, mỗi phần tử có "type": MoneyVip | LinhDan | Item | RandomSkill | RandomSpecialSkill | Skill,
    /// các trường còn lại theo lớp thưởng sự kiện tương ứng (xem Data/dong-phu-achievements.json).
    /// </summary>
    public JArray Rewards { get; set; } = new();

    /// <summary>Tạo bản thưởng mới mỗi lần phát (một số loại thưởng như kỹ năng ngẫu nhiên thay đổi khi nhận).</summary>
    public List<IEventReward> CreateRewards() => Rewards.Select(DongPhuAchievementConfig.ParseReward).ToList();
}

public class DongPhuAchievementDefinition
{
    /// <summary>Id thành tựu (chuỗi ngắn, duy nhất). Đừng đổi sau khi phát hành.</summary>
    public string Id { get; set; } = string.Empty;
    public DongPhuStat Stat { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Mô tả mốc; "{target}" được thay bằng mục tiêu của mốc.</summary>
    public string Description { get; set; } = string.Empty;
    public List<DongPhuAchievementTierConfig> Tiers { get; set; } = new();
}

/// <summary>
/// Cấu hình thành tựu Động phủ đọc từ <c>Data/dong-phu-achievements.json</c>. File được theo dõi: sửa và lưu là server
/// tự nạp lại (không cần build/restart). File lỗi thì giữ cấu hình cũ và ghi log.
/// </summary>
public class DongPhuAchievementConfig
{
    public const string FilePath = "Data/dong-phu-achievements.json";

    public List<DongPhuAchievementDefinition> Achievements { get; set; } = new();

    private static readonly JsonSerializerSettings Settings = new()
    {
        Converters = { new StringEnumConverter() },
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    private static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

    private static readonly Dictionary<string, Type> RewardTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MoneyVip"] = typeof(MoneyVipEventReward),
        ["LinhDan"] = typeof(LinhDanEventReward),
        ["Item"] = typeof(ItemEventReward),
        ["RandomSkill"] = typeof(RandomSkillEventReward),
        ["RandomSpecialSkill"] = typeof(RandomSpecialSkillEventReward),
        ["Skill"] = typeof(SkillEventReward)
    };

    private static volatile DongPhuAchievementConfig current = new();
    private static FileSystemWatcher? watcher;
    private static Timer? reloadTimer;

    public static DongPhuAchievementConfig Current => current;

    /// <summary>
    /// Dựng phần thưởng từ JSON. Trường phân loại là "type" (chữ thường) — KHÁC với trường "Type" (ItemType)
    /// của các thưởng kỹ năng, nên chỉ bỏ đúng khoá "type" trước khi đọc các trường còn lại.
    /// </summary>
    public static IEventReward ParseReward(JToken token)
    {
        var obj = (JObject)token.DeepClone();
        var type = obj.Property("type", StringComparison.Ordinal)?.Value.Value<string>() ??
                   throw new InvalidDataException("Phần thưởng thiếu trường \"type\" (chữ thường)");
        if (!RewardTypes.TryGetValue(type, out var rewardType))
            throw new InvalidDataException($"Loại thưởng không hỗ trợ: {type}");
        obj.Remove("type");
        var reward = (IEventReward)obj.ToObject(rewardType, Serializer)!;
        ValidateReward(reward);
        return reward;
    }

    /// <summary>Thưởng kỹ năng ngẫu nhiên chỉ hỗ trợ Thần thông/Công pháp (client cũng chỉ hiển thị hai loại này).</summary>
    private static void ValidateReward(IEventReward reward)
    {
        var skillType = reward switch
        {
            RandomSpecialSkillEventReward r => (ItemType?)r.Type,
            RandomSkillEventReward r => r.Type,
            _ => null
        };
        if (skillType is { } t && t != ItemType.ThanThong && t != ItemType.CongPhap)
            throw new InvalidDataException($"Thưởng kỹ năng ngẫu nhiên phải có \"Type\": ThanThong hoặc CongPhap (đang là {t})");
    }

    /// <summary>Nạp lần đầu và bật theo dõi file. Gọi một lần khi server khởi động.</summary>
    public static void Initialize()
    {
        Reload();
        try
        {
            var fullPath = Path.GetFullPath(FilePath);
            watcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath)!, Path.GetFileName(fullPath))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
            };
            // Trình soạn thảo thường ghi file nhiều lần liên tiếp: gom lại, nạp sau 1 giây.
            reloadTimer = new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
            FileSystemEventHandler onChanged = (_, _) => reloadTimer.Change(1000, Timeout.Infinite);
            watcher.Changed += onChanged;
            watcher.Created += onChanged;
            watcher.Renamed += (_, _) => reloadTimer.Change(1000, Timeout.Infinite);
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "DongPhu achievements: cannot watch {Path}; edits need a restart", FilePath);
        }
    }

    public static void Reload()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Log.Warning("DongPhu achievements: {Path} not found, no achievements", FilePath);
                return;
            }

            var config = JsonConvert.DeserializeObject<DongPhuAchievementConfig>(File.ReadAllText(FilePath), Settings)
                         ?? new DongPhuAchievementConfig();
            config.Validate();
            current = config;
            Log.Information("DongPhu achievements loaded: {Count} achievements", config.Achievements.Count);
        }
        catch (Exception e)
        {
            Log.Error(e, "DongPhu achievements: invalid {Path}, keep previous config", FilePath);
        }
    }

    /// <summary>Kiểm tra Id trùng, mốc tăng dần và thưởng hợp lệ để lỗi cấu hình lộ ra ngay khi nạp.</summary>
    private void Validate()
    {
        var ids = new HashSet<string>();
        foreach (var a in Achievements)
        {
            if (string.IsNullOrWhiteSpace(a.Id)) throw new InvalidDataException("Thành tựu thiếu Id");
            if (a.Id.Contains(':') || a.Id.Contains(';')) throw new InvalidDataException($"Id {a.Id} không được chứa ':' hoặc ';'");
            if (!ids.Add(a.Id)) throw new InvalidDataException($"Trùng Id thành tựu {a.Id}");
            var tierIds = new HashSet<int>();
            long last = 0;
            foreach (var t in a.Tiers)
            {
                if (!tierIds.Add(t.Id)) throw new InvalidDataException($"{a.Id}: trùng Id mốc {t.Id}");
                if (t.Target <= last) throw new InvalidDataException($"{a.Id}: mục tiêu các mốc phải tăng dần");
                last = t.Target;
                t.CreateRewards();
            }
        }
    }

    public DongPhuAchievementDefinition? Find(string id) => Achievements.FirstOrDefault(x => x.Id == id);
}
