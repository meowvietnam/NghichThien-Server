using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class ChiThuy : NgoDaoCombat, IMemorySerializable<ChiThuy>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref ChiThuy value)
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