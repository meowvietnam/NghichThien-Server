using System.ComponentModel.DataAnnotations;

namespace MTS.Models;

public class Clan
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string? ImageUrl { get; set; }
}