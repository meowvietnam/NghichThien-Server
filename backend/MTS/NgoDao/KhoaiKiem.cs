using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class KhoaiKiem : NgoDaoCombat, IMemorySerializable<KhoaiKiem>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref KhoaiKiem value)
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