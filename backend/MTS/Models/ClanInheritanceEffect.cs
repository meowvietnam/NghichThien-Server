using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using MTS.Game.BaseSkill;

namespace MTS.Models;

public enum ClanInheritanceTrigger : byte
{
    StartTurn = 1,
    EndTurn = 2
}

[MemoryPackable]
public partial class ClanInheritanceEffect
{
    [Key] public int Id { get; set; }
    public int ClanId { get; set; }
    public ClanInheritanceTrigger Trigger { get; set; }
    public int SlotIndex { get; set; }
    public EBuffAndDeBuff BuffType { get; set; }
    public int Value { get; set; }

    [ForeignKey(nameof(ClanId)), MemoryPackIgnore]
    public Clan Clan { get; set; } = null!;
}
