using MemoryPack;
using MTS.Network.Models;

namespace MTS.Game.Boss;
[MemoryPackable]
public partial class BossData
{
    public ETypeVisualChar SkinId { get; private set; }
    public CharacterSnapshot Character { get; private set; }

    public BossData(ETypeVisualChar skinId, CharacterSnapshot character)
    {
        SkinId = skinId;
        Character = character;
    }

    public BossData DeepClone()
    {
        var bytes = MemoryPackSerializer.Serialize(this);
        return MemoryPackSerializer.Deserialize<BossData>(bytes);
    }
}