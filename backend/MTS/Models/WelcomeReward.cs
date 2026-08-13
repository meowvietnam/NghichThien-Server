using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class WelcomeReward
{
    public int CharId { get; set; }
    public int ClaimCount { get; set; }
    public DateTime ClaimDate { get; set; }
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }
    public Mail CreateMail()
    {
        return new Mail()
        {
            Title = $"Quà tân thủ - Ngày {ClaimCount}",
            Content = $"Chúc mừng đạo hữu đã đồng hành ngày thứ {ClaimCount}! Đây là phần quà tân thủ dành cho đạo hữu. Hãy tiếp tục tu luyện và khám phá thế giới!",
            Rewards = new List<IMailReward>()
            {
                new MailRewardMoneyVip()
                {
                    MoneyVip = 1000,
                }
            }
        };
    }
}