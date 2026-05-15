using System;
using Network;
using Network.Extensions;

namespace MTS.CP
{
    public class HonThienThuat : ThoNapCongPhap, IMemorySerializable<HonThienThuat>
    {
        public Int32[] dames;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + dames.ByteSize();
        }

        public void Deserialize(IReader reader, ref HonThienThuat value)
        {
            ThoNapCongPhap baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.dames = reader.ReadArray<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.WriteArray(dames);
        }
    }
}