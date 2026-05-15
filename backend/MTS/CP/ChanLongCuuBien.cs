using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class ChanLongCuuBien : ThoNapCongPhap, IMemorySerializable<ChanLongCuuBien>
    {
        public Int32[] buffValues2;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + buffValues2.ByteSize();
        }

        public void Deserialize(IReader reader, ref ChanLongCuuBien value)
        {
            ThoNapCongPhap baseValue = value;
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