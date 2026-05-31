using System;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class DietLinhQuyet : CongPhap, IMemorySerializable<DietLinhQuyet>
    {
        public Int32[] buffValues;
        public Int32[] buffValues2;
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
            return byteSize + buffValues.ByteSize() + buffValues2.ByteSize();
        }

        public void Deserialize(IReader reader, ref DietLinhQuyet value)
        {
            CongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.buffValues = reader.ReadArray<Int32>();
            value.buffValues2 = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(buffValues);
            writer.WriteArray(buffValues2);
        }
    }
}