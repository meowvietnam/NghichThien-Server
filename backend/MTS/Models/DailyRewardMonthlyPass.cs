using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MTS.Data;

namespace MTS.Models;

public class DailyRewardMonthlyPass
{
    [Key]
    public int Id { get; set; }
    public int CharId { get; set; }
    public List<IMailReward> Rewards { get; set; }
    public DateTime LastClaim { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; }
    [ForeignKey(nameof(CharId))] public CharacterData CharData { get; set; }

    public bool IsClaimedToday()
    {
        return Utils.Utils.IsToday(LastClaim);
    }

    public bool IsEnded()
    {
        return EndDate < DateTime.UtcNow;
    }

    public async Task Claim(CharacterData charData, AppDbContext db)
    {
        LastClaim = DateTime.UtcNow;
        var utcEndDate = DateTime.SpecifyKind(EndDate, DateTimeKind.Utc);
        var endDate = new DateTimeOffset(utcEndDate)
            .ToOffset(TimeSpan.FromHours(7))
            .ToString("dd/MM/yyyy HH:mm");
        var mail = new Mail()
        {
            Title = "Quà Thưởng Thẻ Tháng",
            Content = $"Phần thưởng Thẻ Tháng hôm nay đã được gửi đến bạn. Thẻ Tháng có hiệu lực đến {endDate}. Chúc bạn có những phút giây trải nghiệm vui vẻ!",
            Rewards = Rewards,
        };
        var mailManager = new MailManager(charData, charData.Client);
        await mailManager.SendMail(mail, db);
    }
}