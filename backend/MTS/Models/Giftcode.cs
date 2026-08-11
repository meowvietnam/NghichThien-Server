namespace MTS.Models;

public class Giftcode
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<IMailReward> Rewards { get; set; } = new ();
    public int CanUse { get; set; } = 999_999_999;
    public int TotalUsed { get; set; } = 0;
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime ExpireDate { get; set; } = DateTime.UtcNow.AddYears(100);
}