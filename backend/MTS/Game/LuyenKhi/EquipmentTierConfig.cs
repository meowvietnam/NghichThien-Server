using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class EquipmentTierConfig : IMemorySerializable<EquipmentTierConfig>
    {
        public EquipmentTier Tier { get; private set; }
        public Color Color { get; private set; }
        public int DayCost { get; private set; }
        public void Deserialize(IReader reader, ref EquipmentTierConfig value)
        {
            value.Tier = reader.Read<EquipmentTier>();
            value.Color = reader.Read<Color>();
            value.DayCost = reader.Read<int>();
        }

        public int ByteSize()
        {
            return Tier.ByteSize() + Color.ByteSize() + DayCost.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Tier);
            writer.Write(Color);
            writer.Write(DayCost);
        }
    }
}