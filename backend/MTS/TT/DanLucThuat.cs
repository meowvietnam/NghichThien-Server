using System;
using Network;
using Network.Extensions;

namespace MTS.TT
{
    public class DanLucThuat : ThanThong, IMemorySerializable<DanLucThuat>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref DanLucThuat value)
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