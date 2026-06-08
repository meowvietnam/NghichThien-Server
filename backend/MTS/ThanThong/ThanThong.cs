using MTS.Game.BaseSkill;
using MTS.Game.Resources;
using Network;

namespace MTS.TT;

public class ThanThong : BaseSkill , IMemorySerializable<ThanThong>
{
    public void Deserialize(IReader reader, ref ThanThong value)
    {
        BaseSkill baseValue = value;
        baseValue.Deserialize(reader, ref baseValue);
    }

    public static int GetDaiCanhGioi(int level)
    {
        int daiCanhGioi = ((level - 1) / 3) + 1;
        return daiCanhGioi;
    }
}