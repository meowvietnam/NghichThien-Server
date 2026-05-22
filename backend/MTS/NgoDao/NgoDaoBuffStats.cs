using System;
using MTS.Models;
using MTS.Network.Models;
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

        public override NgoDaoBuff Buff(CharacterData charData)
        {
            var stats = charData.Stats;
            switch (currentEBuffStat)
            {
                case EBuffStats.ThanThuc:
                    stats.ThanThuc += buff;
                    break;
                case EBuffStats.HP:
                    stats.SinhMenh += buff;
                    stats.Hp += buff;
                    break;
                case EBuffStats.DonToc:
                    stats.TocDoBay += buff;
                    break;
            }

            return new NgoDaoBuff()
            {
                BuffStat = currentEBuffStat,
                BuffValue = buff
            };
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.Write(currentEBuffStat);
            writer.Write(buff);
        }
    }
}