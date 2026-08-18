using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IIapReward Reward { get; set; }
    public bool IsActive { get; set; }
    public string Name { get; set; }
    [MemoryPackIgnore]
    public string? Description { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int? LinkerId { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int Priority { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public ProductLinker? Linker { get; set; }
    
}