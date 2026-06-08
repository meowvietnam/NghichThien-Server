using System;
using MTS.Models;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class MaDiemQuyet : CongPhap, IMemorySerializable<MaDiemQuyet>
    {
        public Int32[] thoNguyens;

        public override void EquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var thoNguyen = stats.ThoNguyen - thoNguyens[level - 1];
            stats.SetThoNguyen(thoNguyen);
        }

        public override void UnEquipCongPhap(CharacterData statsCurrent, int level)
        {
            var stats = statsCurrent.Stats;
            var thoNguyen = stats.ThoNguyen + thoNguyens[level - 1];
            stats.SetThoNguyen(thoNguyen);
        }

        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + thoNguyens.ByteSize();
        }

        public void Deserialize(IReader reader, ref MaDiemQuyet value)
        {
            CongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.thoNguyens = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(thoNguyens);
        }
    }
}