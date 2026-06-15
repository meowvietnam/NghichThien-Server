using MemoryPack;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;
[MemoryPackable]
public partial class DanCombatUsed : IEquatable<DanCombatUsed>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public ushort Count { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public EquipmentManager EquipmentManager { get; set; }
    public bool Equals(DanCombatUsed? other)
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