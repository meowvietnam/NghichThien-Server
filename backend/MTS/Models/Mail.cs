using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Network;
using Network.Extensions;
using Newtonsoft.Json;

namespace MTS.Models;

public class Mail : IMemorySerializable<Mail>
{
    [Key] public int Id { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public int CharId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public bool IsClaim { get; set; }
    public List<ItemRef> Rewards { get; set; } = new List<ItemRef>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    [ForeignKey(nameof(CharId))]
    public CharacterData CharacterData { get; set; }

    public void Deserialize(IReader reader, ref Mail value)
    {
        value.Id = reader.Read<int>();
        value.Title = reader.Read();
        value.Content = reader.Read();
        value.IsRead = reader.Read<bool>();
        value.IsClaim = reader.Read<bool>();
        value.Rewards = reader.ReadArray<ItemRef>().ToList();
        value.CreatedAt = reader.Read<DateTime>();
    }

    public int ByteSize()
    {
        return Id.ByteSize() + Title.ByteSize() + Content.ByteSize()
               + IsRead.ByteSize() + IsClaim.ByteSize() + Rewards.ByteSize()
               + CreatedAt.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.Write(Title);
        writer.Write(Content);
        writer.Write(IsRead);
        writer.Write(IsClaim);
        writer.WriteArray(Rewards);
        writer.Write(CreatedAt);
    }
}