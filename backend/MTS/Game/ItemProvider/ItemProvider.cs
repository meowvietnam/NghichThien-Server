using MTS.Game.LuyenDan;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public interface IItemProvider
    {
        ItemType SupportedType { get; }
        int GetPriceItemSell(ushort id);

        int GetPriceItemBuy(ushort id);
        (int,int) ValueTangQuaNPCWithItem(ushort id, int npcLevel);

    }

    public abstract class ItemProvider<T> : IItemProvider
    {
        public abstract T GetDataItemByIdRef(ushort id);
        public abstract ItemType SupportedType { get; }
        public abstract int GetPriceItemSell(ushort id);

        public virtual int GetPriceItemBuy(ushort id)
        {
            return GetPriceItemSell(id) * 3;
        }
        public virtual int BaseValueLevelWithTier => 0;

        public virtual (int, int) ValueTangQuaNPCWithItem(ushort id, int npcLevel)
        {
            var data = GetDataItemByIdRef(id);
            int levelWithTier = BaseValueLevelWithTier;
            int indexTier = 1;

            if (data is IItemData itemData)
            {
                levelWithTier += GameConfig.tierItemWithLevel.GetValueOrDefault(itemData.Tier);
                indexTier = (int)itemData.Tier;

            }
            else if (data is BaseSkill.BaseSkill baseSkill)
            {
                levelWithTier += GameConfig.tierSkillWithLevel.GetValueOrDefault(baseSkill.eTierSkill);
                indexTier = (int)baseSkill.eTierSkill;

            }
            else if (data is Equipment equipment)
            {
                levelWithTier += GameConfig.tierEquipmentWithLevel.GetValueOrDefault(equipment.GradeConfig.Tier);
                indexTier = (int)equipment.GradeConfig.Tier;

            }
            return (levelWithTier - npcLevel, indexTier);


        }
    }
    
}