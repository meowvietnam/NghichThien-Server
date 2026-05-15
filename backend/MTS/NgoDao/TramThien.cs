using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class TramThien : ManKiem, IMemorySerializable<TramThien>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref TramThien value)
        {
            ManKiem baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
        }
    }
}