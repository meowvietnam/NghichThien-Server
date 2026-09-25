using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;

namespace MTS.Models;
[MemoryPackable]
public partial class Puppet
{
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public int ClanId { get; set; }
    public int? NodeId { get; set; }
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }
    [ForeignKey(nameof(ClanId))]
    [MemoryPackIgnore]
    public Clan? Clan { get; set; }
}