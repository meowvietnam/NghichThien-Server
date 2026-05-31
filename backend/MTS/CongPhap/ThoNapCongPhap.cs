using System;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class ThoNapCongPhap : CongPhap, IMemorySerializable<ThoNapCongPhap>
    {
        public Int32[] buffValues;

        public override void EquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var sinhMenh = stats.SinhMenh + buffValues[level - 1];
            stats.SetSinhMenh(sinhMenh);
        }

        public override void UnEquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var sinhMenh = stats.SinhMenh - buffValues[level - 1];
            stats.SetSinhMenh(sinhMenh);
            if (stats.Hp > sinhMenh)
            {
                stats.SetHp(sinhMenh);
            }
        }

        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues.ByteSize();
        }

        public void Deserialize(IReader reader, ref ThoNapCongPhap value)
        {
            CongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.buffValues = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(buffValues);
        }
    }
}