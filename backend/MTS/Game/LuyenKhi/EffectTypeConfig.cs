using MemoryPack;
using Network;
using Network.Extensions;

namespace MTS.Game.LuyenKhi
{
    [Serializable]
    [MemoryPackable]
    public partial class EffectTypeConfig
    {
        public EffectType Type { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }
}