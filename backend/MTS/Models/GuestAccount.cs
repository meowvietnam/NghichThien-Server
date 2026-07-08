using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class GuestAccount
{
    [Key]
    public int Id { get; set; }
    public string UserName { get; set; }
    public int UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public UserData UserData { get; set; }
}