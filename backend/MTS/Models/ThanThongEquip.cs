using System.ComponentModel.DataAnnotations.Schema;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

public class ThanThongEquip : IMemorySerializable<ThanThongEquip>, IEquatable<ThanThongEquip>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public Equipment Equipment { get; set; }
    public void Deserialize(IReader reader, ref ThanThongEquip value)
    {
        value.Id = reader.Read<ushort>();
    }
    public int ByteSize()
    {
        return Id.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
    }

    public bool Equals(ThanThongEquip? other)
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