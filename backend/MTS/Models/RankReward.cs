using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.Game;

namespace MTS.Models;
[MemoryPackable]
public partial class RankReward
{
    [Key]
    public long Id { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public ETypeArenaRank Type { get; set; }
    public bool IsClaim {get; set;}
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    [ForeignKey(nameof(CharId))]
    public ArenaManager ArenaManager { get; set; }
}