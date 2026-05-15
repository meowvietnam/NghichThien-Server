using System;
using Network;
using Network.Extensions;

namespace MTS.TT
{
    public class PhuongLinhYeuHoa : ThanThong, IMemorySerializable<PhuongLinhYeuHoa>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref PhuongLinhYeuHoa value)
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