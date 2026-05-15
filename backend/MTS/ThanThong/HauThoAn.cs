using System;
using Network;
using Network.Extensions;

namespace MTS.TT
{
    public class HauThoAn : ThanThong, IMemorySerializable<HauThoAn>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref HauThoAn value)
        {
            ThanThong baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
        }
    }
}