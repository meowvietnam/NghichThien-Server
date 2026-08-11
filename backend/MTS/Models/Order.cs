using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MTS.Network.Models;

namespace MTS.Models;

public enum OrderStatus
{
    Processing,
    Complete,
}

public class Order
{
    [Key] public long OrderId { get; set; }
    public int ProductId { get; set; }
    public int CharId { get; set; }
    public long Amount { get; set; }
    public int? PurchaseId { get; set; }
    public string Link { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Processing;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public DateTime ExpireAt { get; set; }
    [ForeignKey(nameof(ProductId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IapProduct Product { get; set; }

    [ForeignKey(nameof(CharId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public CharacterData CharData { get; set; }

    [ForeignKey(nameof(PurchaseId))]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IapPurchase? Purchase { get; set; }
}