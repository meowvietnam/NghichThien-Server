using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class TuLanh : NgoDaoCombat, IMemorySerializable<TuLanh>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref TuLanh value)
        {
            NgoDaoCombat baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
        }
    }
}