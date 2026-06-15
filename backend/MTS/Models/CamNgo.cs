using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using MTS.ND;
using Network;
using Network.Extensions;

namespace MTS.Models;

[MemoryPackable]
public partial class CamNgo : IEquatable<CamNgo>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }

    
    public ETypeSkill Id { get; set; }
    public int Value { get; set; }

    
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    [MemoryPackIgnore]
    public InventoryManager Inventory { get; set; }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public bool Equals(CamNgo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
}