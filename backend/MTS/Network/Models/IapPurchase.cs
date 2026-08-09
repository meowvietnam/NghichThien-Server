using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MTS.IAP;
using MTS.Models;

namespace MTS.Network.Models;

public class IapPurchase
{
    [Key]
    public int Id { get; set; }
    public int CharId { get; set; }
    public int ProductId { get; set; }
    public Store Store { get; set; }
    public byte[] PurchaseTokenHash { get; set; }
    [ForeignKey(nameof(ProductId))]
    public IapProduct Product { get; set; }
    public long MoneyVipSnapshot { get; set; } = 0;
    public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
}