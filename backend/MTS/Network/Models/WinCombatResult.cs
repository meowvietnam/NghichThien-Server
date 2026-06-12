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
        public CamNgo[]?NewCamNgo { get; set; } = Array.Empty<CamNgo>(); 

        public int ByteSize()
        {
            int size = Success.ByteSize();
            size += Message != null ? Message.ByteSize() : string.Empty.ByteSize();

            size += Reward != null ? Reward.ByteSize() : 0;
            size += NewHP.ByteSize();
            size += NewCamNgo != null ? NewCamNgo.ByteSize() : 0;
            return size;
        }

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
            writer.Write(Message ?? string.Empty);
            writer.Write(Reward ?? new RewardSnapshot());
            writer.WriteArray(NewCamNgo ?? Array.Empty<CamNgo>());
            writer.Write(NewHP);
        }
    }
}