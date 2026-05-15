using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class TuTuongThienDanLuc : CongPhap, IMemorySerializable<TuTuongThienDanLuc>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref TuTuongThienDanLuc value)
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