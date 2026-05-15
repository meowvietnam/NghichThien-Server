using Network;

namespace MTS.TT;

public class NeTranhTT : ThanThong, IMemorySerializable<NeTranhTT>
{
    public void Deserialize(IReader reader, ref NeTranhTT value)
    {
        ThanThong baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }
}