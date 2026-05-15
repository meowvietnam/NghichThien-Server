using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class BaoThapQuanTuongPhap : CongPhap, IMemorySerializable<BaoThapQuanTuongPhap>
    {
        public Int32[] buffValues;
        public Int32[] buffValues2;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues.ByteSize() + buffValues2.ByteSize();
        }

        public void Deserialize(IReader reader, ref BaoThapQuanTuongPhap value)
        {
            CongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.buffValues = reader.ReadArray<Int32>();
            value.buffValues2 = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(buffValues);
            writer.WriteArray(buffValues2);
        }
    }
}