using System.ComponentModel.DataAnnotations;
using MTS.IAP;

namespace MTS.Models;

public class IapProduct
{
    [Key]
    public int Id { get; set; }
    public string StoreProductId { get; set; }      
    public long Price { get; set; }
    public IapProductType ProductType { get; set; }
    public Store Store { get; set; }
    public IIapReward Reward { get; set; }
    public bool IsActive { get; set; }
}