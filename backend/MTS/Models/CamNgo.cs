using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class CamNgo : IMemorySerializable<CamNgo>, IEquatable<CamNgo>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public ushort Value { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public InventoryManager Inventory { get; set; }
    public void Deserialize(IReader reader, ref CamNgo value)
    {
        value.Id = reader.Read<ushort>();
        value.Value = reader.Read<ushort>();
    }

    public int ByteSize()
    {
        return Id.ByteSize() + Value.ByteSize();
    }
    public override int GetHashCode()
    {
        return Id;
    }
    public bool Equals(CamNgo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Value);
    }


}