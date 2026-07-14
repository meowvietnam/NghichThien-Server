using System.ComponentModel.DataAnnotations;
using MemoryPack;
using MTS.IAP;

namespace MTS.Models;
[MemoryPackable]
public partial class IapProduct
{
    [Key]
    public int Id { get; set; }
    public string StoreProductId { get; set; }      
    public long Price { get; set; }
    public IapProductType ProductType { get; set; }
    [MemoryPackIgnore]
    public IapProductType ActualProductType { get; set; }
    public Store Store { get; set; }
    public IIapReward Reward { get; set; }
    public bool IsActive { get; set; }
}