using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi;

public class Effect : IMemorySerializable<Effect>
{
    public EffectType Type { get; set; }
    public byte Value { get; set; }
    public void Deserialize(IReader reader, ref Effect value)
    {
        value.Type = reader.Read<EffectType>();
        value.Value = reader.Read<byte>();
    }

    public int ByteSize()
    {
        return Type.ByteSize() + Value.ByteSize();
    }

    public void Serialize(IWriter writer)
    {
        writer.Write(Type);
        writer.Write(Value);
    }
}