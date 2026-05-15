using System;
using Network;
using Network.Extensions;

namespace MTS.ND
{
    public class NgoDaoBuffStats : NgoDao, IMemorySerializable<NgoDaoBuffStats>
    {
        public enum EBuffStats
        {
            None,
            ThanThuc,
            HP,
            DonToc,
        }

        public EBuffStats currentEBuffStat;
        public Int32 buff;
        public override int ByteSize()
        {
            var byteSize = base.ByteSize();
            return byteSize + currentEBuffStat.ByteSize() + buff.ByteSize();
        }

        public void Deserialize(IReader reader, ref NgoDaoBuffStats value)
        {
            NgoDao baseValue = value;
            baseValue.Deserialize(reader, ref baseValue);
            value.currentEBuffStat = reader.Read<EBuffStats>();
            value.buff = reader.Read<Int32>();
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.Write(currentEBuffStat);
            writer.Write(buff);
        }
    }
}