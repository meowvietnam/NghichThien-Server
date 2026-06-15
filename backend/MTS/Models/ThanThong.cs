using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;
[MemoryPackable]
public partial class ThanThong : IEquatable<ThanThong>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public InventoryManager Inventory { get; set; }
    public bool Equals(ThanThong? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
    public override int GetHashCode()
    {
        return Id;
    }
}