using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class MaDiemQuyet : CongPhap, IMemorySerializable<MaDiemQuyet>
    {
        public Int32[] thoNguyens;
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