using System.ComponentModel.DataAnnotations.Schema;

namespace MTS.Models;

public class Puppet
{
    public int CharId { get; set; }
    public int ClanId { get; set; }
    public int? NodeId { get; set; }
    [ForeignKey(nameof(CharId))]
    public CharacterData CharData { get; set; }
    [ForeignKey(nameof(ClanId))]
    public Clan? Clan { get; set; }
}