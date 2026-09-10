using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class ClanJoinRequest
{
    public int CharId { get; set; }
    public int ClanId { get; set; }
    [NotMapped] public string Name => CharData.Name;
    [NotMapped] public int Level => CharData.Progression.Level;
    [NotMapped] public Gender Gender => CharData.Gender;
    [NotMapped] public Appearance Appearance => CharData.Appearance;
    [MemoryPackIgnore]
    public CharacterData CharData { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
}