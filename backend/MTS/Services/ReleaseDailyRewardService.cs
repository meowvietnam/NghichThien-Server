using Microsoft.EntityFrameworkCore;
using MTS.Game;
using MTS.Models;
using MTS.Network;
using MTS.Network.Models;
using Network.Extensions;
using static MTS.Utils.Utils;

namespace MTS.Services;

public static class ReleaseDailyRewardService
{
    private const int RewardDays = 8;
    private const int MoneyVipReward = 1000;
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);

    public static async Task<Mail?> TrySendLoginRewardAsync(CharacterData characterData, Client? client = null)
    {
        if (!TryGetRewardDayIndex(Now(), out var rewardDayIndex))
        {
            return null;
        }

        var rewardDay = rewardDayIndex + 1;
        await using var db = await Program.CreateDbAsync();
        var strategy = db.Database.CreateExecutionStrategy();
        Mail? mail = null;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var alreadyClaimed = await db.ReleaseDailyRewardClaims
                .AnyAsync(x => x.CharId == characterData.Id && x.RewardDay == rewardDay);
            if (alreadyClaimed)
            {
                return;
            }

            mail = CreateMail(characterData.Id, rewardDayIndex);
            await db.ReleaseDailyRewardClaims.AddAsync(new ReleaseDailyRewardClaim
            {
                CharId = characterData.Id,
                RewardDay = rewardDay,
                ClaimedAt = DateTime.UtcNow
            });
            await db.Mails.AddAsync(mail);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.Entries.Any(x => x.Entity is ReleaseDailyRewardClaim))
            {
                mail = null;
                return;
            }

            await transaction.CommitAsync();
        });

        if (mail != null)
        {
            client?.Send(MessageTag.NewMail, mail);
        }

        return mail;
    }

    private static bool TryGetRewardDayIndex(DateTimeOffset now, out int rewardDayIndex)
    {
        var campaignStart = GetCampaignStart();
        var campaignEnd = campaignStart.AddDays(RewardDays);
        if (now < campaignStart || now >= campaignEnd)
        {
            rewardDayIndex = -1;
            return false;
        }

        var passedDays = (now - campaignStart).Ticks / Interval.Ticks;
        rewardDayIndex = (int)Math.Clamp(passedDays, 0, RewardDays - 1);
        return true;
    }

    private static DateTimeOffset GetCampaignStart()
    {
        var releaseDate = GameConfig.GameReleaseDate;
        return new DateTimeOffset(
            releaseDate.Year,
            releaseDate.Month,
            releaseDate.Day,
            0,
            0,
            0,
            releaseDate.Offset);
    }

    private static Mail CreateMail(int charId, int rewardDayIndex)
    {
        return new Mail
        {
            CharId = charId,
            Title = $"Quà release ngày {rewardDayIndex + 1}/{RewardDays}",
            Content = $"Cảm ơn đạo hữu đã đăng nhập trong sự kiện release.\n"
                      + $"Phần thưởng ngày {rewardDayIndex + 1}/{RewardDays}: {MoneyVipReward:N0} Tiên ngọc.",
            CreatedAt = DateTime.UtcNow,
            Rewards =
            [
                new MailRewardMoneyVip { MoneyVip = MoneyVipReward }
            ]
        };
    }
}
