using System;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class PhiVanKinh : CongPhap, IMemorySerializable<PhiVanKinh>
    {
        public Int32[] buffValues;
        public override void EquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var tocDoBay = stats.ThanThuc + buffValues[level - 1];
            stats.SetTocDoBay(tocDoBay);
        }

        public override void UnEquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var tocDoBay = stats.ThanThuc - buffValues[level - 1];
            stats.SetTocDoBay(tocDoBay);
        }
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues.ByteSize();
        }

        public void Deserialize(IReader reader, ref PhiVanKinh value)
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