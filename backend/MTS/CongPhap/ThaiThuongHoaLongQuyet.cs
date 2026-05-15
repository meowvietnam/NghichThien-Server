using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class ThaiThuongHoaLongQuyet : ThoNapCongPhap, IMemorySerializable<ThaiThuongHoaLongQuyet>
    {
        public Int32 percent;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + percent.ByteSize();
        }

        public void Deserialize(IReader reader, ref ThaiThuongHoaLongQuyet value)
        {
            ThoNapCongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.percent = reader.Read<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.Write(percent);
        }
    }
}