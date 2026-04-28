using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    public class EffectTypeConfig : IMemorySerializable<EffectTypeConfig>
    {
        public EffectType Type { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
        public void Deserialize(IReader reader, ref EffectTypeConfig value)
        {
            value.Type = reader.Read<EffectType>();
            value.Name = reader.Read();
            value.Value = reader.Read<int>();
        }
        public int ByteSize()
        {
            return Type.ByteSize() + Name.ByteSize() + Value.ByteSize();
        }
        public void Serialize(IWriter writer)
        {
            writer.Write(Type);
            writer.Write(Name);
            writer.Write(Value);
        }
    }
}