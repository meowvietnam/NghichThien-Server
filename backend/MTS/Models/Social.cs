using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MTS.Data;
using MTS.Utils;
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

    [NotMapped] private Modify<Relationship> modifyRelationships = new();
    public void Deserialize(IReader reader, ref Social value)
    {
        value.Mails = reader.ReadArray<Mail>().ToList();
        value.Relationships = reader.ReadArray<Relationship>().ToHashSet();
    }
    public void AddRelationship(Relationship relationship)
    {
        relationship.CharId = CharId;
        Relationships.Add(relationship);
        modifyRelationships.Add(relationship);
    }
    public Relationship GetRelationship(ushort npcId)
    {
        var relationshipRef = new Relationship()
        {
            Npc = npcId,
        };
        if (Relationships.TryGetValue(relationshipRef, out var relationship))
        {
            return relationship;
        }
        return relationshipRef;
    }
    public void RemoveRelationship(Relationship relationship)
    {
        Relationships.Remove(relationship);
        modifyRelationships.Delete(relationship);
    }
    public void RelationshipAddValue(Relationship relationship, int value)
    {
        var newValue = relationship.Points + value;
        relationship.Points = (ushort)newValue;
        modifyRelationships.Update(relationship);
    }
    public void RelationshipAddValue(ushort npcId, int value)
    {
        var relationship = GetRelationship(npcId);
        RelationshipAddValue(relationship, value);
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
        Mails = new(other.Mails);
        Relationships = new(other.Relationships);
        CharacterData = other.CharacterData;
    }

    public bool IsDefault()
    {
        return CharId == 0;
    }

    public Task SaveChanges(AppDbContext db)
    {
        lock (modifyRelationships)
        {
            modifyRelationships.Apply(db.Relationships);
            modifyRelationships.Clear();
        }
        return Task.CompletedTask;
    }
}