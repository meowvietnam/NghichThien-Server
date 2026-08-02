using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class GuestAccount
{
    [Key]
    public int Id { get; set; }
    public string Username { get; set; }
    public string IPAddress { get; set; }
    public string DeviceId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public UserData UserData { get; set; }
}