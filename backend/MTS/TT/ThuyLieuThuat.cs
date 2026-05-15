using System;
using Network;
using Network.Extensions;

namespace MTS.TT
{
    public class ThuyLieuThuat : ThanThong, IMemorySerializable<ThuyLieuThuat>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref ThuyLieuThuat value)
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