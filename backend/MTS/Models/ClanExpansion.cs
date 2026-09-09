namespace MTS.Models;

public enum ClanCreationPaymentStatus : byte { Pending, Paid, Consumed }

// A successful payment grants a creation right. It is deliberately separate from a
// Clan name, so a duplicate name or a full Clan cannot lose a paid order.
public class ClanCreationPayment
{
    public long OrderId { get; set; }
    public int CharId { get; set; }
    public Guid RequestId { get; set; }
    public long Amount { get; set; }
    public string CheckoutUrl { get; set; } = "";
    public string PaymentLinkId { get; set; } = "";
    public string? PaymentReference { get; set; }
    public ClanCreationPaymentStatus Status { get; set; }
    public int? ConsumedByClanId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class ClanInheritance
{
    public int ClanId { get; set; }
    public int InheritanceId { get; set; }
    public int UnlockedByCharId { get; set; }
    public DateTime UnlockedAt { get; set; }
}

public class ClanMemberInheritance
{
    public int CharId { get; set; }
    public int ClanId { get; set; }
    public int InheritanceId { get; set; }
    public DateTime LearnedAt { get; set; }
}
