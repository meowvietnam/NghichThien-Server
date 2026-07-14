using MTS.Models;

namespace MTS.Api.Models;

public class User
{
    public int Id { get; set; }
    public Role Role { get; set; }
    public List<Character> Characters { get; set; }

    public User(UserData userData)
    {
        Id = userData.Id;
        Role = userData.Role;
        Characters = userData.CharacterDatas.Select(character => new Character(character)).ToList();
    }
}