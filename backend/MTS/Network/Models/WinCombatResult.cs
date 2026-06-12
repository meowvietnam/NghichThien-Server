using MTS.Game;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Network.Models
{
    public class WinCombatResult : IMemorySerializable<WinCombatResult>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public RewardSnapshot Reward { get; set; } = new RewardSnapshot();
        public int NewHP { get; set; }
        public CamNgo[] NewCamNgo { get; set; } = Array.Empty<CamNgo>(); // Chuẩn C# dùng Array.Empty

        public int ByteSize()
        {
            int size = Success.ByteSize();

            // Check an toàn cho Message đề phòng bị null
            size += Message != null ? Message.ByteSize() : string.Empty.ByteSize();

            size += Reward != null ? Reward.ByteSize() : 0;

            // Sửa lỗi: Thêm kích thước của NewHP vào đây
            size += NewHP.ByteSize();

            // Check an toàn cho mảng NewCamNgo đề phòng bị null
            size += NewCamNgo != null ? NewCamNgo.ByteSize() : 0;

            return size;
        }

        // ĐÃ ĐỒNG BỘ LẠI THỨ TỰ: GIỐNG HỆT NHƯ HÀM SERIALIZE
        public void Deserialize(IReader reader, ref WinCombatResult value)
        {
            value.Success = reader.Read<bool>();
            value.Message = reader.Read();
            value.Reward = reader.Read<RewardSnapshot>();
            value.NewCamNgo = reader.ReadArray<CamNgo>() ?? Array.Empty<CamNgo>();
            value.NewHP = reader.Read<int>();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Success);
            writer.Write(Message ?? string.Empty); // Phòng hờ null
            writer.Write(Reward ?? new RewardSnapshot());
            writer.WriteArray(NewCamNgo ?? Array.Empty<CamNgo>());
            writer.Write(NewHP);
        }
    }
}
