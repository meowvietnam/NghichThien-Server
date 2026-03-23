using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    public class LoginResult : IMemorySerializable<LoginResult>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserData UserData { get; set; }

        public void Deserialize(IReader reader, ref LoginResult value)
        {
            var success = reader.Read<bool>();
            value.Success = success;
            value.Message = reader.Read();
            if (success)
            {
                UserData = reader.Read<UserData>();
            }
        }

        public int ByteSize()
        {
            if (!Success)
            {
                return Success.ByteSize() + Message.ByteSize();
            }
            return Success.ByteSize() + Message.ByteSize() + UserData.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Success);
            writer.Write(Message);
            if (Success)
            {
                writer.Write(UserData);
            }
        }
    }
}