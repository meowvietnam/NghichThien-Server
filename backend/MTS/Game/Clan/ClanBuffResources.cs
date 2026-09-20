
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
    [MemoryPackable]
    public partial class MinMaxValueThreshold
    {
        public int LevelThreshold { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }

        public (int Min, int Max) GetMinMax()
        {
            return (Min, Max);
        }
    }
    public class ClanBuffResources : IMemorySerializable<ClanBuffResources>
    {
        public EffectTypeConfig[] EffectTypeConfigs { get; private set; }
        public ScoreDivisorRule[] Rules { get; private set; }
        
        public (int Min, int Max) GetBuffValueRangeOf(EffectType? type, int level)
        {
            if (!type.HasValue)
            {
                return (0, 0);
            }
            var effect = EffectTypeConfigs.FirstOrDefault(x => x.Type == type);
            if (effect == null) return (0,0);
            if (effect.OverrideRange)
            {
                return effect.Thresholds.Last(x => level >= x.LevelThreshold).GetMinMax();
            }
            var rule = Rules.FirstOrDefault(x => x.ScoreThreshold >= effect.Value);
            if (rule == null) return (0,0); 
            var baseValue = effect.Value / rule.Divisor;
            var minValue = (baseValue / 2) * level;
            var maxValue = baseValue * 2 * level;
            return (minValue, maxValue);
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
