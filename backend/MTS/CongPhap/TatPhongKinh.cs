using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class TatPhongKinh : PhiVanKinh, IMemorySerializable<TatPhongKinh>
    {
        public Int32[] buffValues2;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues2.ByteSize();
        }

        public void Deserialize(IReader reader, ref TatPhongKinh value)
        {
            PhiVanKinh baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.buffValues2 = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(buffValues2);
        }
    }
}