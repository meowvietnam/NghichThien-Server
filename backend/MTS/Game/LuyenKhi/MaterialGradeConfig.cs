using MTS.Game.LuyenDan;
using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class MaterialGradeConfig : IMemorySerializable<MaterialGradeConfig>
    {
        public Tier Tier { get; private set; }
        public int Grade { get; private set; }
        public void Deserialize(IReader reader, ref MaterialGradeConfig value)
        {
            value.Tier = reader.Read<Tier>();
            value.Grade = reader.Read<int>();
        }
        public int ByteSize()
        {
            return Tier.ByteSize() + Grade.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(Tier);
            writer.Write(Grade);
        }
    }
}