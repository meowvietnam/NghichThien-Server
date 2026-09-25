using MTS.Models;

namespace MTS.Network.Models;

public class SelectCharacterResult : IMemorySerializable<SelectCharacterResult>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public CharacterData CharacterData { get; set; }
    public void Deserialize(IReader reader, ref SelectCharacterResult value)
    {
        value.Success = reader.Read<bool>();
        if (!value.Success)
        {
            value.Message = reader.Read();
        }
        else
        {
            value.CharacterData = reader.Read<CharacterData>();
        }
    }
    public int ByteSize()
    {
        if (Success)
        {
            return Success.ByteSize() + CharacterData.ByteSize();
        }
        else
        {
            return Success.ByteSize() + Message.ByteSize();
        }
    }
    public void Serialize(IWriter writer)
    {
        writer.Write(Success);
        if (!Success)
        {
            writer.Write(Message);
        }
        else
        {
            writer.Write(CharacterData);
        }
    }
}