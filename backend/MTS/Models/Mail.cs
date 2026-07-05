using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;
[MemoryPackable]
public partial class Mail 
{
    [Key] public long Id { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public bool IsClaim { get; set; }
    public List<IMailReward> Rewards { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public Social Social { get; set; }
}