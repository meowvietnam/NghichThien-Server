using MemoryPack;
using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi;
[MemoryPackable]
public partial class Effect
{
    public EffectType Type { get; set; }
    public byte Value { get; set; }

}