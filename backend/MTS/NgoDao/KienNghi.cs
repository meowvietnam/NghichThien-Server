using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class KienNghi : NgoDaoCombat, IMemorySerializable<KienNghi>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref KienNghi value)
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