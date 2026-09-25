using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class ClanNode
{
    [Key]
    public int NodeId { get; set; }
    public int? ClanId { get; set; }
    [ForeignKey(nameof(ClanId))]
    [MemoryPackIgnore]
    public Clan? Clan { get; set; }
}