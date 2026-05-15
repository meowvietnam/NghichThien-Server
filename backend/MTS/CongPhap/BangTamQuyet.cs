using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class BangTamQuyet : CongPhap, IMemorySerializable<BangTamQuyet>
    {
        public Int32[] buffValues;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues.ByteSize();
        }

        public void Deserialize(IReader reader, ref BangTamQuyet value)
        {
            CongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.buffValues = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(buffValues);
        }
    }
}