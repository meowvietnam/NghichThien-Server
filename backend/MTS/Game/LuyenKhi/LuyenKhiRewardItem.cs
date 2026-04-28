using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class LuyenKhiRewardItem : IMemorySerializable<LuyenKhiRewardItem>
    {
        public int Id { get; set; }
        public EquipmentTier Tier { get; set; }
        public ArtifactType Type { get; set; }
        public void Deserialize(IReader reader, ref LuyenKhiRewardItem value)
        {
            value.Id = reader.Read<int>();
            value.Tier = reader.Read<EquipmentTier>();
            value.Type = reader.Read<ArtifactType>();
        }
        public int ByteSize()
        {
            return Id.ByteSize() + Tier.ByteSize() + Type.ByteSize();
        }
        public void Serialize(IWriter writer)
        {
            writer.Write(Id);
            writer.Write(Tier);
            writer.Write(Type);
        }
    }
}