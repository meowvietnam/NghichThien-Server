using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

public class DanCombatUsed : IMemorySerializable<DanCombatUsed>, IEquatable<DanCombatUsed>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public ushort Count { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public EquipmentManager EquipmentManager { get; set; }
    public void Deserialize(IReader reader, ref DanCombatUsed value)
    {
        value.Id = reader.Read<ushort>();
        value.Count = reader.Read<ushort>();
    }
    public int ByteSize()
    {
        return Id.ByteSize() + Count.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Count);
    }
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