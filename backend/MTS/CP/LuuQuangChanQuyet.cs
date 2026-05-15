using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class LuuQuangChanQuyet : CongPhap, IMemorySerializable<LuuQuangChanQuyet>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref LuuQuangChanQuyet value)
        {
            CongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
        }
    }
}