using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class RecipeProvider : ItemProvider<RecipeData>
    {
        public override ItemType SupportedType => ItemType.Recipe;
        public override RecipeData GetDataItemById(ushort Id)
        {
           return LuyenDanResources.Instance.FindRecipe(Id);
        }
        public override int GetPriceItemSell(ushort Id)
        {
            var data = GetDataItemById(Id);
            return data.LinhDan.PriceV130 * 5;
        }
    }
}
