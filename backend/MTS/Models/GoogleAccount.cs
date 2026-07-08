using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class GoogleAccount
{
    [Key]
    public int Id { get; set; }
    public string Email { get; set; }
    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public UserData UserData { get; set; }
}