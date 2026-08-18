using System;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class SiMiQuyQuyen : CPTieuHaoThoNguyen, IMemorySerializable<SiMiQuyQuyen>
    {
        public Int32[] buffValues;
        public Int32[] donTocs;
        public override void EquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var tocDoBay = stats.TocDoBay + donTocs[level - 1];
            stats.SetTocDoBay(tocDoBay);
        }

        public override void UnEquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var tocDoBay = stats.TocDoBay - donTocs[level - 1];
            stats.SetTocDoBay(tocDoBay);
        }
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues.ByteSize() + donTocs.ByteSize();
        }

        public void Deserialize(IReader reader, ref SiMiQuyQuyen value)
        {
            CPTieuHaoThoNguyen baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.buffValues = reader.ReadArray<Int32>();
            value.donTocs = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(buffValues);
            writer.WriteArray(donTocs);
        }
    }
}