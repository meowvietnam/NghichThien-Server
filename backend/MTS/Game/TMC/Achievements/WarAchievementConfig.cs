using MTS.Game.DP.Achievements;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace MTS.Game.TMC.Achievements;

public class WarAchievementDefinition
{
    /// <summary>Id thành tựu (chuỗi ngắn, duy nhất). Đừng đổi sau khi phát hành.</summary>
    public string Id { get; set; } = string.Empty;
    public WarStat Stat { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Mô tả mốc; "{target}" được thay bằng mục tiêu của mốc.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Mốc và phần thưởng, cùng định dạng với thành tựu Động phủ.</summary>
    public List<DongPhuAchievementTierConfig> Tiers { get; set; } = new();
}

/// <summary>
/// Cấu hình thành tựu Tông môn chiến đọc từ <c>Data/war-achievements.json</c> (cùng định dạng mốc/thưởng với thành tựu
/// Động phủ). File được theo dõi: sửa và lưu là server tự nạp lại. File lỗi thì giữ cấu hình cũ và ghi log.
/// </summary>
public class WarAchievementConfig
{
    public const string FilePath = "Data/war-achievements.json";

    public List<WarAchievementDefinition> Achievements { get; set; } = new();

    private static readonly JsonSerializerSettings Settings = new()
    {
        Converters = { new StringEnumConverter() },
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    private static volatile WarAchievementConfig current = new();
    private static FileSystemWatcher? watcher;
    private static Timer? reloadTimer;

    public static WarAchievementConfig Current => current;

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
            reloadTimer = new Timer(_ => Reload(), null, Timeout.Infinite, Timeout.Infinite);
            FileSystemEventHandler onChanged = (_, _) => reloadTimer.Change(1000, Timeout.Infinite);
            watcher.Changed += onChanged;
            watcher.Created += onChanged;
            watcher.Renamed += (_, _) => reloadTimer.Change(1000, Timeout.Infinite);
            watcher.EnableRaisingEvents = true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "War achievements: cannot watch {Path}; edits need a restart", FilePath);
        }
    }

    public static void Reload()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                Log.Warning("War achievements: {Path} not found, no achievements", FilePath);
                return;
            }

            var config = JsonConvert.DeserializeObject<WarAchievementConfig>(File.ReadAllText(FilePath), Settings)
                         ?? new WarAchievementConfig();
            config.Validate();
            current = config;
            Log.Information("War achievements loaded: {Count} achievements", config.Achievements.Count);
        }
        catch (Exception e)
        {
            Log.Error(e, "War achievements: invalid {Path}, keep previous config", FilePath);
        }
    }

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

    public WarAchievementDefinition? Find(string id) => Achievements.FirstOrDefault(x => x.Id == id);
}
