using Microsoft.EntityFrameworkCore;
using MTS.Game.Auction;
using Serilog;

namespace MTS.Services;

public class AuctionService : BackgroundService
{
    private AuctionManager? auctionManager;

    private DayOfWeek[] dayOpens =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
        DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    private TimeOnly[] hourOpen = [new(12, 00), new(21, 30)];

    private string DefaultContent()
    {
        var days = dayOpens
            .Select(x => Utils.Utils.GetDayOfWeek(x))
            .Select(day => Color(day, "#C2410C"))
            .ToArray();
        var hours = hourOpen
            .OrderBy(h => h)
            .Select(h => Color(h.ToString("HH:mm"), "#D97706"))
            .ToArray();
        return $"{Bold(Color("Lịch đấu giá", "#B91C1C"))}\n" +
               $"{Color("Đấu giá được tổ chức vào", "#B45309")} {string.Join(", ", hours)} {string.Join(", ", days)} {Color("hàng tuần", "#B45309")}" +
               "\n" +
               "Tại Hội Đấu Giá (Thiên Cơ Các)";
    }

    private static string Color(string value, string color)
    {
        return $"<color={color}>{value}</color>";
    }

    private static string Bold(string value)
    {
        return $"<b>{value}</b>";
    }

    private async Task Start()
    {
        await using var db = await Program.CreateDbAsync();
        var history = await db.AuctionHistories
            .AsNoTracking()
            .OrderByDescending(h => h.Id)
            .FirstOrDefaultAsync();
        if (history is null)
        {
            auctionManager = new AuctionManager(DefaultContent());
        }
        else
        {
            var content = CreateEndContent(history.StartTime);
            auctionManager = new AuctionManager(history, content);
        }
    }

    private string CreateEndContent(DateTime startTime)
    {
        startTime += Utils.Utils.Utc7;
        var time = Color(startTime.ToString("dd/MM/yyyy HH:mm"), "#D97706");
        var content = DefaultContent() + "\n" +
                      $"{Bold(Color("Kết quả phiên đấu giá", "#B91C1C"))} {time} (Đã kết thúc)";
        return content;
    }

    private DateTimeOffset GetNextOpenTime()
    {
        var now = Utils.Utils.Now();
        var currentDay = now.DayOfWeek;
        var currentDate = now.Date;

        var distinctDays = dayOpens.Distinct().ToArray();
        var distinctHours = hourOpen.Distinct().OrderBy(h => h).ToArray();

        var nextOpenTime = distinctDays
            .SelectMany(day =>
            {
                var dayOffset = ((int)day - (int)currentDay + 7) % 7;
                var date = currentDate.AddDays(dayOffset);
                return distinctHours.Select(hour => new DateTimeOffset(
                    date.Year,
                    date.Month,
                    date.Day,
                    hour.Hour,
                    hour.Minute,
                    hour.Second,
                    now.Offset));
            })
            .Where(openAt => openAt > now)
            .OrderBy(openAt => openAt)
            .FirstOrDefault();

        if (nextOpenTime != default)
        {
            return nextOpenTime;
        }

        var firstOpenDay = distinctDays.OrderBy(day => (int)day).First();
        var firstOpenHour = distinctHours.First();
        var daysToNextWeek = ((int)firstOpenDay - (int)currentDay + 7) % 7;
        if (daysToNextWeek == 0)
        {
            daysToNextWeek = 7;
        }

        var nextWeekDate = currentDate.AddDays(daysToNextWeek);
        return new DateTimeOffset(
            nextWeekDate.Year,
            nextWeekDate.Month,
            nextWeekDate.Day,
            firstOpenHour.Hour,
            firstOpenHour.Minute,
            firstOpenHour.Second,
            now.Offset);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Start();
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextOpenTime = GetNextOpenTime();
            var now = Utils.Utils.Now();
            var delay = nextOpenTime - now;
            await Task.Delay(delay, stoppingToken);
            var content = DefaultContent() + "\n" +
                          Bold(Color("Phiên đấu giá đang diễn ra", "#B91C1C"));
            auctionManager = new AuctionManager(content);
            auctionManager.OnAuctionEnded += OnAuctionEnded;
            await auctionManager.Init();
            Log.Information($"Phiên đấu giá bắt đầu!");
        }
    }

    private void OnAuctionEnded()
    {
        if (auctionManager != null)
        {
            auctionManager.OnAuctionEnded -= OnAuctionEnded;
            var content = CreateEndContent(auctionManager.StartTime);
            auctionManager.SetContent(content);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (auctionManager != null)
        {
            await auctionManager.ForceStop();
        }

        await base.StopAsync(cancellationToken);
    }
}