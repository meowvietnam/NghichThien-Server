using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class EquipmentGradeConfig : IMemorySerializable<EquipmentGradeConfig>
    {
        public int MinGrade { get; private set; }
        public EquipmentTier Tier { get; private set; }
        public EquipmentGrade Grade { get; private set; }
        public string Name { get; private set; }
        public void Deserialize(IReader reader, ref EquipmentGradeConfig value)
        {
            value.MinGrade = reader.Read<int>();
            value.Tier = reader.Read<EquipmentTier>();
            value.Grade = reader.Read<EquipmentGrade>();
        }

        public int ByteSize()
        {
            return MinGrade.ByteSize() + Tier.ByteSize() + Grade.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(MinGrade);
            writer.Write(Tier);
            writer.Write(Grade);
        }
    }
}