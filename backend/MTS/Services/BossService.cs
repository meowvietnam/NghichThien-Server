using MTS.Game;
using MTS.Game.Boss;
using Serilog;

namespace MTS.Services;

public class BossService : BackgroundService
{
    private readonly BossOptions options;
    private long currentSpawnId;

    public BossService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Boss");
        options = section.Exists() ? section.Get<BossOptions>() ?? new BossOptions() : BossOptions.Default;
        if (!options.KeepUntilNextSpawn && options.StartTime == options.EndTime)
            throw new InvalidOperationException("Boss StartTime and EndTime must differ.");
    }

    public static (DateTimeOffset Start, DateTimeOffset End)? GetActiveWindow(
        DateTimeOffset now, IEnumerable<DayOfWeek> daysOfWeek, TimeOnly startTime, TimeOnly endTime,
        bool keepUntilNextSpawn = false)
    {
        if (!keepUntilNextSpawn && startTime == endTime)
            throw new InvalidOperationException("Boss start and end times must differ.");

        var selectedDays = daysOfWeek?.ToHashSet() ?? [];
        var local = now.ToOffset(Utils.Utils.Utc7);
        if (keepUntilNextSpawn)
        {
            for (var offset = 0; offset <= 7; offset++)
            {
                var date = local.Date.AddDays(-offset);
                if (!selectedDays.Contains(date.DayOfWeek)) continue;
                var start = new DateTimeOffset(date.Add(startTime.ToTimeSpan()), Utils.Utils.Utc7);
                if (start <= local) return (start, DateTimeOffset.MaxValue);
            }
            return null;
        }
        for (var offset = -1; offset <= 0; offset++)
        {
            var date = local.Date.AddDays(offset);
            if (!selectedDays.Contains(date.DayOfWeek)) continue;
            var start = new DateTimeOffset(date.Add(startTime.ToTimeSpan()), Utils.Utils.Utc7);
            var end = new DateTimeOffset(date.Add(endTime.ToTimeSpan()), Utils.Utils.Utc7);
            if (end <= start) end = end.AddDays(1);
            if (start <= local && local < end) return (start, end);
        }
        return null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (GameConfig.BossDatas.Length == 0)
        {
            Log.Warning("Boss service disabled: export Data/BossResources.bin first.");
            return;
        }
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var window = GetActiveWindow(DateTimeOffset.UtcNow,
                    options.DaysOfWeek, options.StartTime, options.EndTime,
                    options.KeepUntilNextSpawn);
                if (window == null)
                {
                    if (BossManager.Instance != null)
                    {
                        BossManager.Instance.Close();
                        BossManager.Instance = null;
                    }
                    currentSpawnId = 0;
                }
                else
                {
                    var spawnId = window.Value.Start.UtcDateTime.Ticks;
                    if (spawnId != currentSpawnId)
                    {
                        var progress = await BossStore.LoadOrCreate(spawnId, GameConfig.BossDatas);
                        var next = new BossManager(progress, window.Value.End)
                        {
                            ScheduleText = options.GetScheduleText()
                        };
                        BossManager.Instance?.Close();
                        BossManager.Instance = next;
                        currentSpawnId = spawnId;
                        if (options.KeepUntilNextSpawn)
                            Log.Information("Boss {Name} active from {Start} until the next spawn",
                                next.Character.Name, window.Value.Start);
                        else
                            Log.Information("Boss {Name} active from {Start} until {End}",
                                next.Character.Name, window.Value.Start, window.Value.End);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Cannot start boss slot; retrying in 5 seconds");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        BossManager.Instance?.Close();
        BossManager.Instance = null;
        await base.StopAsync(cancellationToken);
    }
}
