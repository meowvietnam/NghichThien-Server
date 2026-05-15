using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class TangKiemQuyet : CongPhap, IMemorySerializable<TangKiemQuyet>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref TangKiemQuyet value)
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