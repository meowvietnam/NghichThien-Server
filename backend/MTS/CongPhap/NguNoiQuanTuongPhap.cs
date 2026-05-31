using System;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class NguNoiQuanTuongPhap : CongPhap, IMemorySerializable<NguNoiQuanTuongPhap>
    {
        public Int32[] buffValues;
        public override void EquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var thanThuc = stats.ThanThuc + buffValues[level - 1];
            stats.SetThanThuc(thanThuc);
        }

        public override void UnEquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var thanThuc = stats.ThanThuc - buffValues[level - 1];
            stats.SetThanThuc(thanThuc);
        }
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues.ByteSize();
        }

        public void Deserialize(IReader reader, ref NguNoiQuanTuongPhap value)
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