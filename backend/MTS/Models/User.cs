using System.ComponentModel.DataAnnotations;

namespace MTS.Models;

public class User
{
    [Key]
    public int Id { get; set; }
}