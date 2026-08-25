using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class LinhDanProvider : ItemProvider<LinhDan>
    {
        public override int BaseValueLevelWithTier => 6;

        public override ItemType SupportedType => ItemType.LinhDan;

        public override LinhDan GetDataItemByIdRef(ushort id)
        {
            return LuyenDanResources.Instance.FindLinhDan(id);
        }
        public override int GetPriceItemSell(ushort id)
        {
            var data = GetDataItemByIdRef(id);
            return data.PriceV130;
        }
    }
}