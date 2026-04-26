using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MTS.Data;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class Social : IMemorySerializable<Social>
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int CharId { get; set; }
    public List<Mail> Mails { get; set; } = new();
    public HashSet<Relationship> Relationships { get; set; } = new();
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharacterData { get; set; }
    
    public void Deserialize(IReader reader, ref Social value)
    {
        value.Mails = reader.ReadArray<Mail>().ToList();
        value.Relationships = reader.ReadArray<Relationship>().ToHashSet();
    }
    public int ByteSize()
    {
        return Mails.ByteSize() + Relationships.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.WriteArray(Mails);
        writer.WriteArray(Relationships);
    }

    public void CopyFrom(Social other)
    {
        CharId = other.CharId;
        Mails = new (other.Mails);
        Relationships = new (other.Relationships);
        CharacterData = other.CharacterData;
    }
    public bool IsDefault()
    {
        return CharId == 0;
    }

    public async Task SaveChanges(AppDbContext db)
    {
        
    }
}