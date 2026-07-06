using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Newtonsoft.Json;

namespace MTS.Models;

public enum SpendFor
{
    Gacha = 1,
}
[MemoryPackable]
public partial class SpendingMoneyVip
{
    [Key] public long Id { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public SpendFor SpendFor { get; set; }
    public long MoneyVip { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public CharacterData CharData { get; set; }
}