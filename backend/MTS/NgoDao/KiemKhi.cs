using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class KiemKhi : NgoDaoCombat, IMemorySerializable<KiemKhi>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref KiemKhi value)
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