using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class VanAnhLuc : CPTieuHaoThoNguyen, IMemorySerializable<VanAnhLuc>
    {
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref VanAnhLuc value)
        {
            CPTieuHaoThoNguyen baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
        }
    }
}