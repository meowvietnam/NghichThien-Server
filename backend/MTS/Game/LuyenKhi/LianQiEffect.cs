using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class LianQiEffect : IMemorySerializable<LianQiEffect>
    {
        public CultivationElement CultivationElement { get; set; }
        public YinYangType YinYangType { get; set; }
        public ArtifactEffect[] Effects { get; set; }
        public void Deserialize(IReader reader, ref LianQiEffect value)
        {
            value.CultivationElement = reader.Read<CultivationElement>();
            value.YinYangType = reader.Read<YinYangType>();
            value.Effects = reader.ReadArray<ArtifactEffect>();
        }

        public int ByteSize()
        {
            return CultivationElement.ByteSize() + YinYangType.ByteSize() + Effects.ByteSize();
        }

        public void Serialize(IWriter writer)
        {
            writer.Write(CultivationElement);
            writer.Write(YinYangType);
            writer.WriteArray(Effects);
        }
    }
}