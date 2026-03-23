using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    public class ConnectResult : IMemorySerializable<ConnectResult>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public void Deserialize(IReader reader, ref ConnectResult value)
        {
            value.Success = reader.Read<bool>();
            value.Message = reader.Read();
            value.Id = reader.Read<int>();
            value.Key = reader.Read();
        }
        public int ByteSize()
        {
            return Success.ByteSize() + Message.ByteSize() + Id.ByteSize() + Key.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Success);
            writer.Write(Message);
            writer.Write(Id);
            writer.Write(Key);
        }
    }
}