using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class ThongMoc : NgoDaoCombat, IMemorySerializable<ThongMoc>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref ThongMoc value)
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