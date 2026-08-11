using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MTS.IAP;

namespace MTS.Models;

public class Promotion
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Active { get; set; } = true;
    public Store Store { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public IPromotionApplicable PromotionApplicable { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}