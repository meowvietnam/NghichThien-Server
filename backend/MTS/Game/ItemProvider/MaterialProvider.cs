using MTS.Game.LuyenKhi;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class MaterialProvider : ItemProvider<Material>
    {
        public override ItemType SupportedType => ItemType.Material;


        public override Material GetDataItemByIdRef(ushort id)
        {
            return LuyenKhiResources.Instance.FindMaterial(id);
        }
        public override int GetPriceItemSell(ushort id)
        {
            return GameConfig.PriceVatLieuWithTier[GetDataItemByIdRef(id).Tier];
        }
    }
}