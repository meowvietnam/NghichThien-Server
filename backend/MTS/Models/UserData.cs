using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MTS.Data;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class UserData : IMemorySerializable<UserData>
{
    [Key] public int Id { get; set; }
    public List<CharacterData> CharacterDatas { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [NotMapped] private bool dirty;
    public UserData()
    {
        CharacterDatas = new List<CharacterData>();
    }
    public int ByteSize()
    {
        return Id.ByteSize() + CharacterDatas.ByteSize() + CreatedAt.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.WriteArray(CharacterDatas);
        writer.Write(CreatedAt);
    }
    public void Deserialize(IReader reader, ref UserData value)
    {
        value.Id = reader.Read<int>();
        value.CharacterDatas = reader.ReadArray<CharacterData>().ToList();
        value.CreatedAt = reader.Read<DateTime>();
    }

    public async Task SaveChanges(AppDbContext db)
    {
        if (!dirty)
        {
            return;
        }
        // await db.Users
        //     .Where(x => x.Id == Id)
        //     .ExecuteUpdateAsync(s => s
        //         .SetProperty(x => x.IsInfiniteEnergy, IsInfiniteEnergy));
        dirty = false;
    }
}