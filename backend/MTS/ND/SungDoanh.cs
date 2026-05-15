using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class SungDoanh : NgoDaoCombat, IMemorySerializable<SungDoanh>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref SungDoanh value)
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