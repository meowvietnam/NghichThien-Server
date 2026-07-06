using MTS.Models;
using MTS.Network;

namespace MTS.Services;

public record TopReward(CharacterData[] Tops, int MoneyVip, string TopDetail);

public static class RankRewardMailer
{
    public static async Task SendAsync(
        IEnumerable<TopReward> rewards,
        string rankName,
        CancellationToken cancellationToken)
    {
        var onlineCharacters = TcpServer.Instance.GetCharsOnline().ToDictionary(x => x.Id);
        foreach (var reward in rewards)
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(reward.Tops, options, async (rankedCharacter, _) =>
            {
                var character = rankedCharacter;
                Client? client = null;
                if (onlineCharacters.TryGetValue(rankedCharacter.Id, out var onlineCharacter))
                {
                    character = onlineCharacter;
                    client = onlineCharacter.Client;
                }

                var mail = new Mail
                {
                    Title = $"Phần thưởng xếp hạng {rankName}",
                    Content = $"Chúc mừng đạo hữu đã đạt {reward.TopDetail} trong mùa xếp hạng {rankName}. " +
                              $"Phần thưởng {reward.MoneyVip:N0} Tiên ngọc đã được gửi kèm theo thư này.",
                    Rewards = new()
                    {
                        new MailRewardMoneyVip { MoneyVip = reward.MoneyVip }
                    }
                };
                await new MailManager(character, client).SendMail(mail);
            });
        }
    }
}
