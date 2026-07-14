namespace MTS.Api.Models;

public class Client
{
    public int Id { get; set; }
    public User? User { get; set; }
    public Character? Character { get; set; }

    public Client(Network.Client client)
    {
        Id = client.Id;
        User = client.UserData != null ? new User(client.UserData) : null;
        Character = client.CharacterData != null ? new Character(client.CharacterData) : null;
    }
}