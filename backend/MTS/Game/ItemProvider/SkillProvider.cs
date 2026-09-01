using MTS.Game.Resources;
using MTS.Models;

namespace MTS.Game.ItemProvider
{
    public abstract class SkillProvider : ItemProvider<BaseSkill.BaseSkill>
    {
        public override int BaseValueLevelWithTier => 3;

        public override int GetPriceItemSell(ushort Id)
        {
            var data = GetDataItemByIdRef(Id);
            if (data != null)
            {
                return GameConfig.PriceTTWithTier[data.eTierSkill];
            }

            return 0;
        }
    }

    public class ThanThongBooProvider : SkillProvider
    {
        public override ItemType SupportedType => ItemType.ThanThong;

        public override BaseSkill.BaseSkill GetDataItemByIdRef(ushort Id)
        {
            return GameConfig.NDCPTTManager.GetThanThongWithId(Id);
        }
    }

    public class CongPhapBookProvider : SkillProvider
    {
        public override ItemType SupportedType => ItemType.CongPhap;

        public override BaseSkill.BaseSkill GetDataItemByIdRef(ushort Id)
        {
            return GameConfig.NDCPTTManager.GetCongPhapWithId(Id);
        }
    }
}