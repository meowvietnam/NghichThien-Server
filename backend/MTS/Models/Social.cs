using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MemoryPack;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using MTS.Utils;
using Network;
using Network.Extensions;

namespace MTS.Models;
[MemoryPackable]
public partial class Social
{
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    public int CharId { get; set; }
    
    public List<Mail> Mails { get; set; } = new();
    public HashSet<Relationship> Relationships { get; set; } = new();
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharacterData { get; set; }

    [NotMapped]
    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [MemoryPackIgnore]
    private Modify<Relationship> modifyRelationships = new();
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
        AddRelationship(relationshipRef);
        return relationshipRef;
    }

    public void RemoveRelationship(Relationship relationship)
    {
        relationship.CharId = CharId;
        if (Relationships.Remove(relationship))
        {
            modifyRelationships.Delete(relationship);
        }
    }

    public void RelationshipAddValue(Relationship relationship, int value)
    {
        var newValue = relationship.Points + value;
        relationship.Points = newValue;
        modifyRelationships.Update(relationship);
    }

    public void RelationshipAddValue(ushort npcId, int value)
    {
        var relationship = GetRelationship(npcId);
        RelationshipAddValue(relationship, value);
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
            modifyRelationships.Apply(db, db.Relationships);
            modifyRelationships.Clear();
        }
        return Task.CompletedTask;
    }
}
