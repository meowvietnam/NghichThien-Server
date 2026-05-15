using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class ThaiThuongHoaLongChanQuyet : ThoNapCongPhap, IMemorySerializable<ThaiThuongHoaLongChanQuyet>
    {
        public Int32[] buffValues2;
        public Int32[] maxs;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues2.ByteSize() + maxs.ByteSize();
        }

        public void Deserialize(IReader reader, ref ThaiThuongHoaLongChanQuyet value)
        {
            ThoNapCongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.buffValues2 = reader.ReadArray<Int32>();
            value.maxs = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(buffValues2);
            writer.WriteArray(maxs);
        }
    }
}