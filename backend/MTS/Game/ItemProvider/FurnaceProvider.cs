using MTS.Game.LuyenDan;
using MTS.Models;
using MTS.Network;

namespace MTS.Game.ItemProvider
{
    public class FurnaceProvider : ItemProvider<FurnaceData>
    {
        public override ItemType SupportedType => ItemType.Furnace;
        private InventoryManager inventoryManager;
        public FurnaceProvider(InventoryManager inventoryManager)
        {
            this.inventoryManager = inventoryManager;
        }
        public override FurnaceData GetDataItemById(ushort id)
        {
            return inventoryManager.Furnaces.FirstOrDefault(x => x.SelfId == id);
        }

        public override int GetPriceItemSell(ushort id)
        {
            FurnaceData furnaceData = GetDataItemById(id);
            return furnaceData.Durability * GameConfig.PriceFurnaceWithTierPerDurability[LuyenDanResources.Instance.FindItem(furnaceData).Tier];
        }
    }
}
