using System.ComponentModel.DataAnnotations;
using Network;
using Network.Extensions;

namespace MTS.Models;

public class UserData : IMemorySerializable<UserData>
{
    [Key] public int Id { get; set; }
    public List<CharacterData> CharacterDatas { get; private set; }
    public UserData()
    {
        CharacterDatas = new List<CharacterData>();
    }
    public int ByteSize()
    {
        return Id.ByteSize() + CharacterDatas.ByteSize();
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Id);
        writer.WriteArray(CharacterDatas);
    }
    public void Deserialize(IReader reader, ref UserData value)
    {
        value.Id = reader.Read<int>();
        value.CharacterDatas = reader.ReadArray<CharacterData>().ToList();
    }
}