using System.ComponentModel.DataAnnotations.Schema;
using MemoryPack;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;
[MemoryPackable]
public partial class ThanThongEquip :  IEquatable<ThanThongEquip>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public byte SlotIndex { get; set; }
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [MemoryPackIgnore]
    public EquipmentManager EquipmentManager { get; set; }
    public bool Equals(ThanThongEquip? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}