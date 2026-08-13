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

        private NgoDaoBuff Buff(CharacterData charData, int value)
        {
            var stats = charData.Stats;
            switch (currentEBuffStat)
            {
                case EBuffStats.ThanThuc:
                    var thanThuc = stats.ThanThuc + value;
                    stats.SetThanThuc(thanThuc);
                    break;
                case EBuffStats.HP:
                    var sinhMenh = stats.SinhMenh + value;
                    var hp = stats.Hp + value;
                    stats.SetSinhMenh(sinhMenh);
                    stats.SetHp(hp);
                    break;
                case EBuffStats.DonToc:
                    var dontoc = stats.TocDoBay + value;
                    stats.SetTocDoBay(dontoc);
                    break;
            }

            return new NgoDaoBuff()
            {
                BuffStat = currentEBuffStat,
                BuffValue = value
            };
        }
        public override NgoDaoBuff Buff(CharacterData charData)
        {
            return Buff(charData, buff);
        }

        public override void UnBuff(CharacterData charData)
        {
            Buff(charData, -buff);
        }

        public override void Serialize(IWriter writer)
        {
            base.Serialize(writer);
            writer.Write(currentEBuffStat);
            writer.Write(buff);
        }
    }
}