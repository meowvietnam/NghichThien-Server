using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class Relationship : IMemorySerializable<Relationship>, IEquatable<Relationship>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }
    public ushort Npc { get; set; }
    public ushort Points { get; set; }
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public Social Social { get; set; }
    public void Deserialize(IReader reader, ref Relationship value)
    {
        value.Npc = reader.Read<ushort>();
        value.Points = reader.Read<ushort>();
    }

    public int ByteSize()
    {
        return Npc.ByteSize() + Points.ByteSize();
    }

    public override int GetHashCode()
    {
        return Npc;
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Npc);
        writer.Write(Points);
    }

    public bool Equals(Relationship? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Npc == other.Npc;
    }
}