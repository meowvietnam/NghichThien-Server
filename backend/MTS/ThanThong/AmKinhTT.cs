using Network;

namespace MTS.TT;

public class AmKinhTT : ThanThong, IMemorySerializable<AmKinhTT>
{
    public void Deserialize(IReader reader, ref AmKinhTT value)
    {
        ThanThong baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }
}