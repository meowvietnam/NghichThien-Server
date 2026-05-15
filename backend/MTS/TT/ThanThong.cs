using Network;

namespace MTS.TT;

public class ThanThong : BaseData , IMemorySerializable<ThanThong>
{
    public void Deserialize(IReader reader, ref ThanThong value)
    {
        BaseData baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }
}