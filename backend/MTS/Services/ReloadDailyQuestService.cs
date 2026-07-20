using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Models;
using MTS.Network;
using static MTS.Utils.Utils;
namespace MTS.Services;

public class ReloadDailyQuestService : BackgroundService
{
    private readonly ILogger<ReloadDailyQuestService> logger;
    private static TimeSpan Interval = TimeSpan.FromDays(1);
    public ReloadDailyQuestService(ILogger<ReloadDailyQuestService> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayToNextRun();
            // logger.LogInformation(
            //     "{Service} sẽ chạy sau {Delay}", nameof(ReloadDailyQuestService), delay);
            await Task.Delay(delay, stoppingToken);

            try
            {
                await RunJobAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi khi chạy {Service}", nameof(ReloadDailyQuestService));
            }
        }
    }

    private async Task RunJobAsync(CancellationToken stoppingToken)
    {
        const int batchSize = 500;
        var processedCount = 0;
        var failedCount = 0;
        var lastCharId = 0;
        var stopwatch = Stopwatch.StartNew();

        await using (var db = await Program.CreateDbAsync())
        {
            var totalCount = await db.QuestManagers.CountAsync(stoppingToken);
            logger.LogInformation(
                "Bắt đầu reload daily quest cho {TotalCount} nhân vật",
                totalCount);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var db = await Program.CreateDbAsync();
            var charIds = await db.QuestManagers
                .Where(x => x.CharId > lastCharId)
                .OrderBy(x => x.CharId)
                .Select(x => x.CharId)
                .Take(batchSize)
                .ToListAsync(stoppingToken);
            if (charIds.Count == 0)
            {
                break;
            }

            lastCharId = charIds[^1];
            var onlineChars = TcpServer.Instance
                .GetCharsOnline()
                .ToDictionary(x => x.Id);
            var dailyQuests = await db.DailyQuests
                .Where(x => charIds.Contains(x.CharId))
                .OrderBy(x => x.CharId)
                .ThenBy(x => x.EDailyQuest)
                .ThenBy(x => x.Id)
                .ToListAsync(stoppingToken);
            var dailyQuestsByCharId = dailyQuests
                .GroupBy(x => x.CharId)
                .ToDictionary(
                    x => x.Key,
                    x => x.GroupBy(q => q.EDailyQuest)
                        .ToDictionary(q => q.Key, q => q.OrderBy(v => v.Id).ToList()));

            foreach (var charId in charIds)
            {
                stoppingToken.ThrowIfCancellationRequested();
                try
                {
                    if (!dailyQuestsByCharId.TryGetValue(charId, out var quests))
                    {
                        logger.LogWarning(
                            "Bỏ qua reload daily quest cho nhân vật {CharacterId} vì không tìm thấy daily quest",
                            charId);
                        failedCount++;
                        continue;
                    }

                    if (onlineChars.TryGetValue(charId, out var charData))
                    {
                        lock (charData)
                        {
                            charData.QuestManager.ReloadDailyQuests();
                            CopyDailyQuestState(charData.QuestManager.DailyQuests, quests);
                        }
                    }
                    else
                    {
                        var questManager = new QuestManager
                        {
                            CharId = charId,
                            DailyQuests = quests
                        };
                        questManager.ReloadDailyQuests();
                    }

                    processedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogError(
                        ex,
                        "Không thể reload daily quest cho nhân vật {CharacterId}",
                        charId);
                }
            }

            await db.SaveChangesAsync(stoppingToken);

            logger.LogInformation(
                "Đã reload daily quest: {ProcessedCount} thành công, {FailedCount} thất bại; batch hiện tại {BatchCount} nhân vật, lưu DB 1 lần",
                processedCount,
                failedCount,
                charIds.Count);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "Hoàn tất reload daily quest: {ProcessedCount} thành công, {FailedCount} thất bại trong {Elapsed}",
            processedCount,
            failedCount,
            stopwatch.Elapsed);
    }

    private static TimeSpan GetDelayToNextRun()
    {
        var start = GameConfig.GameReleaseDate;
        start = new DateTimeOffset(start.Year, start.Month, start.Day, 0, 0, 0, start.Offset);
        var now = Now();
        var next = GetNextPeriodicTime(start, now, Interval);
        return next - now;
    }

    private static void CopyDailyQuestState(
        Dictionary<EDailyQuest, List<DailyQuest>> source,
        Dictionary<EDailyQuest, List<DailyQuest>> target)
    {
        var targetById = target.Values
            .SelectMany(x => x)
            .ToDictionary(x => x.Id);
        foreach (var sourceQuest in source.Values.SelectMany(x => x))
        {
            if (!targetById.TryGetValue(sourceQuest.Id, out var targetQuest))
            {
                continue;
            }

            targetQuest.IsRewardClaimed = sourceQuest.IsRewardClaimed;
            targetQuest.QuestReward = sourceQuest.QuestReward;
            targetQuest.LastReload = sourceQuest.LastReload;
        }
    }
}
