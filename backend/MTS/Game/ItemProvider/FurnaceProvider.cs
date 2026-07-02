using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public class FurnaceProvider : ItemProvider<FurnaceData>
    {
        public virtual int BaseValueLevelWithTier => 3;
        public override ItemType SupportedType => ItemType.Furnace;
        private InventoryManager inventoryManager;
        public FurnaceProvider(InventoryManager inventoryManager)
        {
            this.inventoryManager = inventoryManager;
        }
        public override FurnaceData GetDataItemByIdRef(ushort id)
        {
            var furnaceData = new FurnaceData()
            {
                SelfId = id
            };
            if (inventoryManager.Furnaces.TryGetValue(furnaceData, out var furnace))
            {
                return furnace;
            }
            return null;
        }

        public override int GetPriceItemSell(ushort id)
        {
            FurnaceData furnaceData = GetDataItemByIdRef(id);
            return furnaceData.Durability * GameConfig.PriceFurnaceWithTierPerDurability[LuyenDanResources.Instance.FindItem(furnaceData).Tier];
        }
        public override (int, int) ValueTangQuaNPCWithItem(ushort id, int npcLevel)
        {
            int currentDurability = GetDataItemByIdRef(id).Durability;
            int lostDurability = Math.Clamp(100 - currentDurability, 0, 100);
            int penalty = lostDurability / 25;
            return (base.ValueTangQuaNPCWithItem(id, npcLevel).Item1 - penalty, base.ValueTangQuaNPCWithItem(id, npcLevel).Item2);
        }
    }
}
