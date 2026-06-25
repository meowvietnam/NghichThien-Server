using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class HerbProvider : ItemProvider<HerbData>
    {
        public override ItemType SupportedType => ItemType.Herb;


        public override HerbData GetDataItemByIdRef(ushort id)
        {
            return LuyenDanResources.Instance.FindHerb(id);
        }

        public override int GetPriceItemSell(ushort id)
        {
            return GameConfig.PriceHerbWithTier[GetDataItemByIdRef(id).Tier];
        }
    }
}