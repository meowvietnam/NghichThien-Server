using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MTS.Data;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class UserData : IMemorySerializable<UserData>
{
    [Key] public int Id { get; set; }
    public List<CharacterData> CharacterDatas { get; set; }
    public bool IsInfiniteEnergy { get; set; }
    public UserData()
    {
        CharacterDatas = new List<CharacterData>();
    }
    public int ByteSize()
    {
        return Id.ByteSize() + CharacterDatas.ByteSize() + IsInfiniteEnergy.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.WriteArray(CharacterDatas);
        writer.Write(IsInfiniteEnergy);
    }
    public void Deserialize(IReader reader, ref UserData value)
    {
        value.Id = reader.Read<int>();
        value.CharacterDatas = reader.ReadArray<CharacterData>().ToList();
        value.IsInfiniteEnergy = reader.Read<bool>();
    }

    public async Task SaveChanges(AppDbContext db)
    {
        var user = await db.Users.FirstAsync(x => x.Id == Id);
        user.IsInfiniteEnergy = IsInfiniteEnergy;
    }
}