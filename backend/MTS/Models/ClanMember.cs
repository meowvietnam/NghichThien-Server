using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class ClanMember
{
    public int ClanId { get; set; }
    public int CharId { get; set; }
    public long ContribPoints { get; set; }
    public DateTime LastFreeContributionAt { get; set; }
    public ClanRole Role { get; set; }
    public DateTime JoinAt { get; set; } = DateTime.UtcNow;
    [NotMapped] public string Name => CharData.Name;
    [NotMapped] public int Level => CharData.Progression.Level;
    [NotMapped] public Gender Gender => CharData.Gender;
    [NotMapped] public Appearance Appearance => CharData.Appearance;
    // Lifetime contributions for this membership; spending only changes ContribPoints.
    public long TotalContribPoints { get; set; }
    [MemoryPackIgnore] public CharacterData CharData { get; set; }

    public bool HasPermission(ClanPermission permission)
    {
        var permissions = ClanRolePermissions.GetPermissions(Role);
        return permissions.HasFlag(permission);
    }
}
