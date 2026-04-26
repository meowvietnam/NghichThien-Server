using System.ComponentModel.DataAnnotations.Schema;
using MTS.Interfaces;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

public class NgoDao : IMemorySerializable<NgoDao>, IEquatable<NgoDao>
{
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }
    public ushort Id { get; set; }
    [ForeignKey(nameof(CharId))]
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public InventoryManager Inventory { get; set; }

    public void Deserialize(IReader reader, ref NgoDao value)
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
    public bool Equals(NgoDao? other)
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