using System;
using Network;
using Network.Extensions;

namespace MTS.TT
{
    public class TrienMocQuyet : ThanThong, IMemorySerializable<TrienMocQuyet>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref TrienMocQuyet value)
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