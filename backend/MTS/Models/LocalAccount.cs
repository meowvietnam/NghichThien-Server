using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class LocalAccount
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; }
    [ForeignKey(nameof(UserId))]
    public UserData UserData { get; set; }
}