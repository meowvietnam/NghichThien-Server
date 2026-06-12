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
        public CamNgo[] NewCamNgo { get; set; }
        public void Deserialize(IReader reader, ref WinCombatResult value)
        {
            value.Success = reader.Read<bool>();
            value.Message = reader.Read();
            value.Reward = reader.Read<RewardSnapshot>();
            value.NewHP = reader.Read<int>();
            value.NewCamNgo = reader.ReadArray<CamNgo>();

        }
        public int ByteSize()
        {
            return Success.ByteSize() + Message.ByteSize() + Reward.ByteSize() + NewCamNgo.ByteSize();
        }
        public void Serialize(IWriter writer)
        {
            writer.Write(Success);
            writer.Write(Message);
            writer.Write(Reward);
            writer.WriteArray(NewCamNgo);
            writer.Write(NewHP);
        }
    }
}
