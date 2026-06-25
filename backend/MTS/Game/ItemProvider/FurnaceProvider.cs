using MTS.Game.LuyenDan;
using MTS.Models;
using MTS.Network;
using MTS.Utils;

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
            return inventoryManager.Furnaces.FirstOrDefault(x => x.SelfId == id);
        }

        public override int GetPriceItemSell(ushort id)
        {
            FurnaceData furnaceData = GetDataItemByIdRef(id);
            return furnaceData.Durability * GameConfig.PriceFurnaceWithTierPerDurability[LuyenDanResources.Instance.FindItem(furnaceData).Tier];
        }
        public override int ValueTangQuaNPCWithItem(ushort id, int npcLevel)
        {
            int currentDurability = GetDataItemByIdRef(id).Durability;
            // Tính số độ bền bị mất, đảm bảo không bị âm và tối đa là 100
            int lostDurability = Math.Clamp(100 - currentDurability, 0, 100);
            // Cứ mỗi 25 điểm mất đi thì tính là 1 điểm phạt
            int penalty = lostDurability / 25;

            return base.ValueTangQuaNPCWithItem(id, npcLevel) - penalty;
        }
    }
}
