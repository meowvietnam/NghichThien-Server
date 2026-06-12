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
            int size = Success.ByteSize();

            if (!string.IsNullOrEmpty(Message))
            {
                size += Message.ByteSize();
            }
            else
            {
                size += string.Empty.ByteSize();
            }

            // Cộng 1 byte lưu trạng thái true/false (Null-Flag) của Client
            size += sizeof(bool);
            if (Client != null)
            {
                size += Client.ByteSize();
            }

            return size;
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Success);
            writer.Write(Message ?? string.Empty);

            // Ghi cờ báo null: true nếu có dữ liệu, false nếu null
            bool hasClient = Client != null;
            writer.Write(hasClient);
            if (hasClient)
            {
                // Gọi hàm Serialize nội bộ của đối tượng để ghi nhị phân chuẩn
                Client.Serialize(writer);
            }
        }

        public void Deserialize(IReader reader, ref LoadCharCombatResult value)
        {
            value.Success = reader.Read<bool>();
            value.Message = reader.Read();

            // Đọc cờ báo null
            bool hasClient = reader.Read<bool>();
            if (hasClient)
            {
                if (value.Client == null) value.Client = new CharacterSnapshot();

                var clientRef = value.Client;
                // Gọi đúng hàm giải mã nhị phân nội bộ của CharacterSnapshot
                clientRef.Deserialize(reader, ref clientRef);
                value.Client = clientRef;
            }
            else
            {
                value.Client = null!;
            }
        }
    }
}