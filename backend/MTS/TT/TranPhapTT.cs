using Network;

namespace MTS.TT;

public class TranPhapTT : ThanThong, IMemorySerializable<TranPhapTT>
{
    public void Deserialize(IReader reader, ref TranPhapTT value)
    {
        ThanThong baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }
}