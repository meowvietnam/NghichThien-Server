using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class HuyenAnhThanHanhQuyet : PhiVanKinh, IMemorySerializable<HuyenAnhThanHanhQuyet>
    {
        public Int32[] percents;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + percents.ByteSize();
        }

        public void Deserialize(IReader reader, ref HuyenAnhThanHanhQuyet value)
        {
            PhiVanKinh baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.percents = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(percents);
        }
    }
}