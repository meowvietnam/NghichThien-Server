using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class NhatKhiPhanThanCong : CongPhap, IMemorySerializable<NhatKhiPhanThanCong>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref NhatKhiPhanThanCong value)
        {
            CongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
        }
    }
}