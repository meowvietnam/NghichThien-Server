using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    public class LoadCharCombatResult : IMemorySerializable<LoadCharCombatResult>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public CharacterSnapshot Client { get; set; }

        public int ByteSize()
        {
            // 1. Tính size của Success (bool = 1 byte)
            int size = Success.ByteSize();

            // 2. Tính size của Message (Phòng hờ null)
            if (!string.IsNullOrEmpty(Message))
            {
                size += Message.ByteSize();
            }
            else
            {
                size += string.Empty.ByteSize();
            }

            if (Client != null)
            {
                size += Client.ByteSize();
            }


            return size;
        }

        public void Serialize(IWriter writer)
        {
            // 1. Ghi Success
            writer.Write(Success);

            // 2. Ghi Message (Nếu null thì ghi chuỗi rỗng để không bị lệch byte)
            writer.Write(Message ?? string.Empty);

            writer.Write(Client);



        }

        public void Deserialize(IReader reader, ref LoadCharCombatResult value)
        {
            // 1. Đọc Success
            value.Success = reader.Read<bool>();

            // 2. Đọc Message
            value.Message = reader.Read();
            value.Client = reader.Read<CharacterSnapshot>();



        }
    }
}
