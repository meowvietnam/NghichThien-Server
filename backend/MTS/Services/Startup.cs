using Microsoft.EntityFrameworkCore;
using MTS.Models;
using MTS.Network;
using static MTS.Utils.Utils;

namespace MTS.Services;

public class Startup : BackgroundService
{
    private readonly ILogger<Startup> logger;

    public Startup(ILogger<Startup> logger)
    {
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = Now();
        var time = GetTimeExecute();
        var delay = time - now;
        if (delay >= TimeSpan.Zero)
        {
            logger.LogInformation("Starting {time}...", delay);
            await Task.Delay(delay, stoppingToken);
        }
        else
        {
            return;
        }

        await using var db = await Program.CreateDbAsync();
        var onlineChars = TcpServer.Instance
            .GetCharsOnline();
        var ids = onlineChars.Select(x => x.Id).ToList();
        var characters = await db.Characters
            .Where(x => !ids.Contains(x.Id))
            .ToListAsync();
        var lst = onlineChars.Concat(characters).ToList();
        var moneyVip = 200_000;
        var listTask = new List<Task>();
        foreach (var character in lst)
        {
            var mailManager = new MailManager(character, character.Client);
            var mail = new Mail()
            {
                Title = $"Quà chào mừng",
                Content = $"Chào mừng {character.Name} tham gia Nghịch Thiên!",
                Rewards = new List<IMailReward>()
                {
                    new MailRewardMoneyVip()
                    {
                        MoneyVip = moneyVip,
                    }
                }
            };
            listTask.Add(mailManager.SendMail(mail));
        }
        await Task.WhenAll(listTask);
    }

    private static DateTimeOffset GetTimeExecute()
    {
        var time = new DateTimeOffset(2026, 7, 16, 21, 0, 0, 0, Utc7);
        return time;
    }
}