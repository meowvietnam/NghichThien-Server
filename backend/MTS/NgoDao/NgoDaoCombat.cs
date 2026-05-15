using Network;

namespace MTS.ND;

public class NgoDaoCombat : NgoDao, IMemorySerializable<NgoDaoCombat>
{
    public void Deserialize(IReader reader, ref NgoDaoCombat value)
    {
        NgoDao baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }
}