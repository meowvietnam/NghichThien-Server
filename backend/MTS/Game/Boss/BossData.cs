using MemoryPack;
using MTS.Game.Rank;

namespace MTS.Game.Boss;
[MemoryPackable]
public partial class BossData
{
    public int SkinId { get; private set; }
    public CharacterWrapper CharWrapper { get; private set; }

    public BossData DeepClone()
    {
        var bytes = MemoryPackSerializer.Serialize(this);
        return MemoryPackSerializer.Deserialize<BossData>(bytes);
    }
}