using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;

namespace MTS.Models;

[MemoryPackable]
public partial class MonsterRankReward
{
    [Key] 
    public long Id { get; set; }
    [MemoryPackIgnore] 
    public int CharId { get; set; }
    public bool IsClaim { get; set; }
    public int RequirePoints { get; set; }
    public List<IMonsterRankReward> Rewards { get; set; } = new ();
    [MemoryPackIgnore]
    [ForeignKey(nameof(CharId))]
    public MonsterKill MonsterKill { get; set; }
}