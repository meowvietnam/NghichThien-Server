using System.ComponentModel.DataAnnotations;

namespace MTS.Models;

public class ProductLinker
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool CanBuyAgain { get; set; }
    public int NextDayCanBuy { get; set; } = int.MaxValue;
    public List<IapProduct> Products { get; set; }
}