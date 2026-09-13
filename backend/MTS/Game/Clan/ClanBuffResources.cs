
using System.Diagnostics;
using System.Net.Mime;
using MemoryPack;
using MTS.Game.LuyenKhi;

namespace MTS.Game.Clan
{
    [MemoryPackable]
    [Serializable]
    public partial class ScoreDivisorRule
    {
        public int ScoreThreshold { get; private set; }
        public int Divisor { get; private set; }
    }
    public class ClanBuffResources : IMemorySerializable<ClanBuffResources>
    {
        public EffectTypeConfig[] EffectTypeConfigs { get; private set; }
        public ScoreDivisorRule[] Rules { get; private set; }
        
        public int GetBaseValueOf(EffectType type)
        {
            var effect = EffectTypeConfigs.FirstOrDefault(x => x.Type == type);
            if (effect == null) return 0;
            var rule = Rules.FirstOrDefault(x => x.ScoreThreshold >= effect.Value);
            if (rule == null) return 0;
            return effect.Value / rule.Divisor;
        }
        public void Deserialize(IReader reader, ref ClanBuffResources value)
        {
            value.EffectTypeConfigs = reader.ReadArray<EffectTypeConfig>();
            value.Rules = reader.ReadArray<ScoreDivisorRule>();
        }
        public int ByteSize()
        {
            return EffectTypeConfigs.ByteSize() + Rules.ByteSize();
        }
        public void Serialize(IWriter writer)
        {
            writer.WriteArray(EffectTypeConfigs);
            writer.WriteArray(Rules);
        }
    }
}
