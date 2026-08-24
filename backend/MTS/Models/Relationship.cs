using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using Network;
using Network.Extensions;

namespace MTS.Models;
[MemoryPackable]
public partial class Relationship : IEquatable<Relationship>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public ushort Npc { get; set; }
    public ushort Points { get; set; }
    public override int GetHashCode()
    {
        return Npc;
    }
    public bool Equals(Relationship? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Npc == other.Npc;
    }
}