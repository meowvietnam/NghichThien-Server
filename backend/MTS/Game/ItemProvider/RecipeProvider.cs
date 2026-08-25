using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class RecipeProvider : ItemProvider<RecipeData>
    {
        public override int BaseValueLevelWithTier => 6;

        public override ItemType SupportedType => ItemType.Recipe;
        public override RecipeData GetDataItemByIdRef(ushort Id)
        {
           return LuyenDanResources.Instance.FindRecipe(Id);
        }
        public override int GetPriceItemSell(ushort Id)
        {
            var data = GetDataItemByIdRef(Id);
            return data.LinhDan.PriceV130 * 5;
        }
    }
}
