using Microsoft.EntityFrameworkCore;
using MTS.Api.Models;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Serilog;
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

    }
    public static Mail CreateWelcomeMail2(CharacterData character)
    {
        var moneyVip = 200_001;
        var mail = new Mail()
        {
            Title = $"Quà cuối tuần",
            Content = $"Phần thưởng {moneyVip} được đính kèm thư này",
            Rewards = new List<IMailReward>()
            {
                new MailRewardMoneyVip()
                {
                    MoneyVip = moneyVip,
                }
            }
        };
        return mail;
    }
    public static Mail CreateWelcomeMail(CharacterData character)
    {
        var moneyVip = 200_000;
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
        return mail;
    }

    public static async Task SendMailWelcome(CharacterData characterData)
    {
        var mailManager = new MailManager(characterData, characterData.Client);
        await mailManager.SendMail(CreateWelcomeMail(characterData));
    }
}