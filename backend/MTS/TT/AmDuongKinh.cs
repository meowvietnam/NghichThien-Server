using System;
using Network;
using Network.Extensions;

namespace MTS.TT
{
    public class AmDuongKinh : AmKinhTT, IMemorySerializable<AmDuongKinh>
    {
        public enum TypeAmDuongKinh
        {
            AmDuongKinh,
            PhongLinhKinh,
            AmLoiKinh
        }

        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize;
        }

        public void Deserialize(IReader reader, ref AmDuongKinh value)
        {
            AmKinhTT baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
        }
    }
}