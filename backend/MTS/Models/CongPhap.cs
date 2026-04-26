using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class CongPhap : IMemorySerializable<CongPhap>, IEquatable<CongPhap>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    public ushort Level { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public InventoryManager Inventory { get; set; }
    public void Deserialize(IReader reader, ref CongPhap value)
    {
        value.Id = reader.Read<ushort>();
        value.Level = reader.Read<ushort>();
    }

    public override int GetHashCode()
    {
        return Id;
    }
    public bool Equals(CongPhap? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
    public int ByteSize()
    {
        return Id.ByteSize() + Level.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Level);
    }


}