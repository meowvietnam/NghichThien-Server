using Network;
using Network.Extensions;

namespace MTS.Network.Models;

public class GetCharacterOnlineThisNodeResult : IMemorySerializable<GetCharacterOnlineThisNodeResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public CharacterSnapshot[] CharactersOnline { get; set; }

    public void Deserialize(IReader reader, ref GetCharacterOnlineThisNodeResult value)
    {
        var success = reader.Read<bool>();
        value.Success = success;
        if (success)
        {
            value.CharactersOnline = reader.ReadArray<CharacterSnapshot>();
        }
        else
        {
            value.Message = reader.Read();
        }
    }

    public int ByteSize()
    {
        if (Success)
        {
            return Success.ByteSize() + CharactersOnline.ByteSize();
        }
        else
        {
            return Success.ByteSize() + Message.ByteSize();
        }
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (Success)
        {
            writer.WriteArray(CharactersOnline);
        }
        else
        {
            writer.Write(Message);
        }
    }
}