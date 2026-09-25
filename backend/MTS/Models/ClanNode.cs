using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class ClanNode
{
    [Key]
    public int NodeId { get; set; }
    public int? ClanId { get; set; }
    [ForeignKey(nameof(ClanId))]
    public Clan? Clan { get; set; }
}