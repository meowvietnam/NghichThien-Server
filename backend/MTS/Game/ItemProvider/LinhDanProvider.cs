using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class LinhDanProvider : ItemProvider<LinhDan>
    {
        public override ItemType SupportedType => ItemType.LinhDan;

        public override LinhDan GetDataItemById(ushort id)
        {
            return LuyenDanResources.Instance.FindLinhDan(id);
        }
        public override int GetPriceItemSell(ushort id)
        {
            var data = GetDataItemById(id);
            return data.PriceV130;
        }
    }
}